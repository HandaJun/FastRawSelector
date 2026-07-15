using FastRawSelector.MANAGER;
using FastRawSelector.MODEL;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 이미지 목록·현재 위치·디코드·프리패치·선택 상태의 오케스트레이션 허브.
    /// 디스크 스캔 → ImageList 구축 → 현재 장 디코드 → NearLoad/AllLoad 프리패치 →
    /// UI(SetImage/SetExif) 갱신 흐름을 담당한다. 상태는 static 전역이다.
    /// </summary>
    public class LoadImage
    {

        public static SortedDictionary<string, ImageData> ImageList = new SortedDictionary<string, ImageData>();
        public static ImageData NowImage = null;
        /// <summary>UI·백그라운드 스레드 공유. 가시성 보장용 volatile.</summary>
        public static volatile int NowIndex = -1;
        public static int LastIndex = -1;
        public static int size = 1616;
        /// <summary>디코드 진행 중 경로. 중복 로드 방지. try/finally 로 반드시 제거.</summary>
        public static ConcurrentDictionary<string, string> processingSet = new ConcurrentDictionary<string, string>();
        public static string NowDir = null;
        public static string settingPath = null;
        public static bool IsImageLoaded = false;

        /// <summary>폴더 전환 시 증가. AllLoad·Export 등 취소용 (P-2/P-3, Part 2).</summary>
        private static int _folderGen = 0;

        /// <summary>현재 폴더 세대. Export/복사가 폴더 전환 후 옛 목록을 쓰지 않게 비교 (Part 2).</summary>
        public static int FolderGen
        {
            get { return _folderGen; }
        }

        /// <summary>NearLoad 재시작 시 증가. 이웃 프리패치만 취소 (AllLoad 유지) (P-2).</summary>
        private static int _nearGen = 0;

        /// <summary>NearLoad 이웃 반경 (현재 인덱스 기준 ±N). PgUp/Dn(10) 대비 넉넉히.</summary>
        private const int NearLoadRadius = 16;

        /// <summary>
        /// ImageArray 안전 상한 (비상용). 일반 폴더(수백~수천 장)는 전부 메모리에 유지해
        /// PgDn/이동 시 재디코드 없이 즉시 표시. 1616 JPEG 기준 약 수백 MB~1GB 대.
        /// 이 값을 넘는 초대형 폴더만 먼 장부터 해제.
        /// </summary>
        private const int ImageArrayMaxCount = 4000;

        /// <summary>
        /// Trim 시에도 현재 위치 ± 이 범위는 유지 (상한 초과 시에만 의미 있음).
        /// </summary>
        private const int ImageArrayKeepRadius = 64;

        /// <summary>
        /// AllLoad·Export 공통 최대 병렬도.
        /// 논리 코어의 약 65% (대략 CPU 6~7할). UI·디스크 여유를 남기며 프리로드 가속.
        /// </summary>
        public static int PrefetchMaxDop
        {
            get
            {
                int n = Environment.ProcessorCount;
                if (n <= 1)
                {
                    return 1;
                }
                if (n == 2)
                {
                    return 2;
                }
                // round(n * 0.65), 최소 2
                int dop = (n * 65 + 50) / 100;
                if (dop < 2)
                {
                    dop = 2;
                }
                if (dop > n)
                {
                    dop = n;
                }
                return dop;
            }
        }

        /// <summary>호환 별칭.</summary>
        private static int AllLoadMaxDop
        {
            get { return PrefetchMaxDop; }
        }

        /// <summary>
        /// ImageArray 가 채워진 직후 호출. 상한 초과 시 뷰포트 밖 장 해제 (Part 1).
        /// EXIF·IsNotImage 는 건드리지 않음 — 재진입 시 ImageArray 만 재디코드.
        /// </summary>
        private static void AfterImageArrayCached()
        {
            TrimImageArrayCache();
        }

        /// <summary>캐시된 ImageArray 개수 (스냅샷, 잠금 없음).</summary>
        private static int CountCachedImageArrays()
        {
            int n = 0;
            try
            {
                foreach (var kv in ImageList)
                {
                    if (kv.Value != null && kv.Value.ImageArray != null)
                    {
                        n++;
                    }
                }
            }
            catch
            {
                // 폴더 전환 중 열거 실패 무시
            }
            return n;
        }

        /// <summary>
        /// 상한 초과 시 |index-NowIndex| 가 큰 장부터 ImageArray 해제.
        /// KeepRadius 안·NowImage·디코드 중(path)은 유지.
        /// </summary>
        private static void TrimImageArrayCache()
        {
            try
            {
                int center = NowIndex;
                int max = ImageArrayMaxCount;
                int keep = ImageArrayKeepRadius;

                var outside = new List<ImageData>();
                int totalCached = 0;
                foreach (var kv in ImageList)
                {
                    ImageData d = kv.Value;
                    if (d == null || d.ImageArray == null)
                    {
                        continue;
                    }
                    totalCached++;
                    if (center < 0 || Math.Abs(d.Index - center) > keep)
                    {
                        outside.Add(d);
                    }
                }

                if (totalCached <= max)
                {
                    return;
                }

                // 현재 위치에서 먼 순
                outside.Sort((a, b) =>
                {
                    int da = Math.Abs(a.Index - center);
                    int db = Math.Abs(b.Index - center);
                    return db.CompareTo(da);
                });

                int need = totalCached - max;
                int freed = 0;
                long freedBytes = 0;
                foreach (var d in outside)
                {
                    if (freed >= need)
                    {
                        break;
                    }
                    if (d == null || d.ImageArray == null)
                    {
                        continue;
                    }
                    if (NowImage != null && object.ReferenceEquals(d, NowImage))
                    {
                        continue;
                    }
                    // 이동 직후 center 가 바뀌었을 수 있음 — 재확인
                    if (Math.Abs(d.Index - NowIndex) <= keep)
                    {
                        continue;
                    }
                    if (processingSet.ContainsKey(d.Path))
                    {
                        continue;
                    }
                    freedBytes += d.ImageArray.LongLength;
                    d.ImageArray = null;
                    freed++;
                }

                if (freed > 0)
                {
                    Log.Debug($"ImageArray 해제: freed={freed}, ~{freedBytes / 1024}KB, remain≈{totalCached - freed}, center={NowIndex}");
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>
        /// AllLoad 프리패치 여부. 미로드·유효 장이면 전부 채움 (속도 우선).
        /// 상한(ImageArrayMaxCount) 초과 시 Trim 이 사후 정리.
        /// </summary>
        private static bool ShouldPrefetchForAllLoad(ImageData data)
        {
            if (data == null)
            {
                return false;
            }
            if (data.IsNotImage)
            {
                return false;
            }
            if (data.ImageArray != null && data.ImageArray.Length > 0)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 폴더를 스캔하고 지정 경로 이미지를 현재 장으로 연다.
        /// 부작용: ImageList/NowIndex/NowDir 교체, SelectorSetting 로드, NearLoad+AllLoad 시작.
        /// </summary>
        /// <param name="path">열 파일 전체 경로 (폴더가 아니라 파일)</param>
        public static void SetArg(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            Log.Info($"폴더 로드 시작: {path}");

            // 이전 폴더 Near/All 프리패치 무효화 (P-2)
            Interlocked.Increment(ref _folderGen);
            Interlocked.Increment(ref _nearGen);
            IsImageLoaded = false;
            Common.Main.IsWindowReady = false;
            Common.Main.SelectImageFolderBt.Visibility = Visibility.Collapsed;
            Common.Main.LoadingGrid.Visibility = Visibility.Visible;


            string dir = System.IO.Path.GetDirectoryName(path);
            NowDir = dir;
            settingPath = Path.Combine(NowDir, "FastRawSelector.yaml");

            if (File.Exists(System.IO.Path.Combine(NowDir, "FastRawSelector.yaml")))
            {
                Common.NowSelectorSetting = SelectorSetting.Load(NowDir);
            }
            else
            {
                Common.NowSelectorSetting = null;
            }

            List<string> files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly).ToList();

            ImageList = new SortedDictionary<string, ImageData>();

            foreach (var file in files)
            {
                if (Common.IsRawFile(file))
                {
                    ImageList.Add(file,
                            new ImageData()
                            {
                                Path = file,
                                FileName = System.IO.Path.GetFileName(file),
                                IsRaw = true
                            }
                        );
                }
                else if (App.Setting.AllowNotRawImage && Common.IsBitmapFile(file))
                {
                    ImageList.Add(file,
                            new ImageData()
                            {
                                Path = file,
                                FileName = System.IO.Path.GetFileName(file),
                                IsBitmap = true
                            }
                        );
                }
            }

            UpdateSelectedCount();

            // NowImage Setting
            for (int i = 0; i < ImageList.Count; i++)
            {
                var item = ImageList.ElementAt(i);
                item.Value.Index = i;
                if (item.Key == path)
                {
                    NowIndex = i;
                    NowImage = item.Value;
                    // P-1: 첫 표시는 썸네일만 (EXIF 지연)
                    var sw = System.Diagnostics.Stopwatch.StartNew();
                    if (NowImage.IsRaw)
                    {
                        var thum = RawManager.GetThumbnailArray(path, size, loadExif: false);
                        NowImage.ImageArray = thum.Item1;
                    }
                    else if (NowImage.IsBitmap)
                    {
                        var thum = BitmapManager.GetBitmapArray(path, size, loadExif: false);
                        NowImage.ImageArray = thum.Item1;
                    }
                    if (NowImage.ImageArray != null)
                    {
                        AfterImageArrayCached();
                    }
                    sw.Stop();
                    Log.Info($"첫 장 썸네일: {sw.ElapsedMilliseconds}ms, path={path}");

                    SetImage(NowImage);
                }
            }

            LastIndex = ImageList.Count - 1;

            // EXIF 패널 표시 상태 적용 후, 필요 시 지연 로드
            ChangeExif(App.Setting.IsExifVisible);
            SetExif(NowImage);
            if (App.Setting.IsExifVisible)
            {
                EnsureExifAsync(NowImage);
            }

            Common.Main.ShowViewControl();

            NearLoad(AllLoad);

            Common.Main.IsWindowReady = true;

            int selectedCount = 0;
            if (Common.NowSelectorSetting != null)
            {
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                }
            }

            // 최근 폴더 저장 (R-1)
            if (App.Setting != null && !string.IsNullOrEmpty(NowDir))
            {
                try
                {
                    if (App.Setting.LastFolderPath != NowDir)
                    {
                        App.Setting.LastFolderPath = NowDir;
                        App.Setting.Save(false);
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }

            // 그리드 표시 중이면 트리에 현재 폴더 펼침/선택
            if (Common.NowView == ViewEnum.Grid && Common.Main != null)
            {
                Common.Invoke(() => Common.Main.GridViewCtrl.SyncTreeToCurrentFolder());
            }

            Log.Info($"폴더 로드 완료: dir={NowDir}, count={ImageList.Count}, index={NowIndex}, selected={selectedCount}, AllowNotRawImage={App.Setting.AllowNotRawImage}");
        }

        /// <summary>
        /// 현재 인덱스 ±NearLoadRadius 이웃을 백그라운드에서 프리패치한다 (가까운 순).
        /// afterAct 는 이웃 큐잉 직후 호출(보통 AllLoad). 세대 번호 시 중단 (P-2).
        /// </summary>
        public static void NearLoad(Action afterAct = null)
        {
            int nearGen = Interlocked.Increment(ref _nearGen);
            int folderGen = _folderGen;
            int center = NowIndex;
            int last = LastIndex;

            Thread t = new Thread(() =>
            {
                try
                {
                    // i=0 은 현재 장(이미 있을 수 있음), 이후 ±1, ±2 …
                    for (int i = 0; i <= NearLoadRadius; i++)
                    {
                        if (nearGen != _nearGen || folderGen != _folderGen)
                        {
                            Log.Debug("NearLoad 중단 nearGen=" + nearGen);
                            break;
                        }
                        for (int j = 0; j < 2; j++)
                        {
                            // j=0: +i, j=1: -i (i=0 은 한 번만)
                            if (i == 0 && j == 1)
                            {
                                continue;
                            }
                            int index = center + (j == 0 ? i : -i);
                            if (index < 0 || index > last)
                            {
                                continue;
                            }
                            if (index >= ImageList.Count)
                            {
                                continue;
                            }
                            ImageData data = ImageList.ElementAt(index).Value;
                            if (processingSet.ContainsKey(data.Path))
                            {
                                continue;
                            }
                            if (data.IsNotImage || data.ImageArray != null)
                            {
                                continue;
                            }
                            if (!processingSet.TryAdd(data.Path, null))
                            {
                                continue;
                            }
                            // 프리패치: EXIF 없이 썸네일만 (P-1)
                            GetThum(data, false, loadExif: false);
                        }
                    }

                    // afterAct(AllLoad)는 폴더가 같으면 이웃 중단 여부와 무관히 1회 기동
                    // (화살표 연타로 Near 가 끊겨도 전체 프리로드는 시작되게)
                    if (folderGen == _folderGen && afterAct != null)
                    {
                        afterAct.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            });
            t.IsBackground = true;
            t.Priority = ThreadPriority.AboveNormal; // Highest 대신 — UI 반응성 (P-2)
            t.Start();
        }

        /// <summary>
        /// 목록 전체를 제한 병렬로 디코드한다. NearLoad 이후에 이어서 호출.
        /// MaxDegreeOfParallelism 상한 (P-3). 폴더 전환 시에만 취소.
        /// </summary>
        public static void AllLoad()
        {
            int folderGen = _folderGen;
            int dop = AllLoadMaxDop;
            Task.Run(() =>
            {
                int count = 0;
                int total = ImageList.Count;
                Log.Info($"전체 프리로드 시작: total={total}, maxDop={dop}");
                var opts = new ParallelOptions { MaxDegreeOfParallelism = dop };
                try
                {
                    Parallel.ForEach(ImageList, opts, (f, state) =>
                    {
                        if (folderGen != _folderGen)
                        {
                            state.Stop();
                            return;
                        }
                        try
                        {
                            if (!processingSet.ContainsKey(f.Key))
                            {
                                if (processingSet.TryAdd(f.Key, null))
                                {
                                    // 전체 장을 메모리에 채움 (이동·PgDn 즉시 표시)
                                    if (ShouldPrefetchForAllLoad(f.Value))
                                    {
                                        GetThumNotThread(f.Value, loadExif: false);
                                    }
                                    else
                                    {
                                        processingSet.TryRemove(f.Key, out _);
                                    }
                                }
                            }
                            Interlocked.Increment(ref count);
                            if (count % 10 == 0 && total > 0)
                            {
                                Common.Invoke(() =>
                                {
                                    try
                                    {
                                        Common.Main.LoadingPb.Value = Math.Min(100d, ((double)count / total) * 100d);
                                    }
                                    catch (Exception ex)
                                    {
                                        Log.Exception(ex);
                                    }
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            processingSet.TryRemove(f.Key, out _);
                            Log.ExceptionWithMsg(f.Key, ex);
                        }
                    });
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }

                if (folderGen != _folderGen)
                {
                    Log.Info("전체 프리로드 취소(폴더 전환)");
                    return;
                }

                Common.Invoke(() =>
                {
                    Common.Main.LoadingGrid.Visibility = Visibility.Collapsed;
                    IsImageLoaded = true;
                    // 목록 재구성(RefreshItems) 하지 않음 — ScrollToPath(현재 장) 로 스크롤이 맨 위로 튀는 문제 방지.
                    // 보이는 칸 썸네일만 갱신 (ImageArray 채워진 것 반영).
                    if (Common.NowView == ViewEnum.Grid && Common.Main != null)
                    {
                        Common.Main.GridViewCtrl.RefreshVisibleThumbnails();
                    }
                });
                Log.Info($"전체 프리로드 완료: total={total}, loaded≈{count}");
            });
        }

        /// <summary>
        /// 단일 이미지를 백그라운드 스레드에서 디코드.
        /// IsSetImage=true 이고 아직 현재 인덱스면 UI에 표시한다.
        /// loadExif=false 가 기본(프리패치). 표시 시 EXIF 는 EnsureExifAsync (P-1).
        /// </summary>
        public static void GetThum(ImageData data, bool IsSetImage, bool loadExif = false)
        {
            if (data == null)
            {
                return;
            }
            // 표시 의도 스냅샷 (디코드 중 NowIndex 가 바뀌어도 경로로 판별)
            int wantIndex = data.Index;
            string wantPath = data.Path;

            new Thread(() =>
            {
                try
                {
                    Thread.Sleep(10);
                    // 이미 다른 장으로 이동했으면 디코드 생략 (호출 측에서 NowIndex 를 먼저 맞출 것)
                    if (IsSetImage && !IsStillCurrent(wantIndex, wantPath))
                    {
                        return;
                    }
                    // 표시용 현재 장은 EXIF 가시 시 함께 로드 가능
                    bool needExif = loadExif || (IsSetImage && App.Setting != null && App.Setting.IsExifVisible);
                    if (data.IsRaw)
                    {
                        var thum = RawManager.GetThumbnailArray(data.Path, size, loadExif: needExif);
                        if (thum.Item1 == null)
                        {
                            MarkDecodeFail(data);
                        }
                        else
                        {
                            data.ImageArray = thum.Item1;
                            if (thum.Item2 != null)
                            {
                                data.Exif = thum.Item2;
                            }
                            MarkDecodeOk(data);
                            AfterImageArrayCached();
                        }
                    }
                    else if (data.IsBitmap)
                    {
                        var bm = BitmapManager.GetBitmapArray(data.Path, size, loadExif: needExif);
                        if (bm.Item1 == null)
                        {
                            MarkDecodeFail(data);
                        }
                        else
                        {
                            data.ImageArray = bm.Item1;
                            if (bm.Item2 != null)
                            {
                                data.BitmapExif = bm.Item2;
                            }
                            MarkDecodeOk(data);
                            AfterImageArrayCached();
                        }
                    }

                    if (IsSetImage && IsStillCurrent(wantIndex, wantPath))
                    {
                        SetImage(data);
                        SetExif(data);
                        if (!needExif && App.Setting != null && App.Setting.IsExifVisible)
                        {
                            EnsureExifAsync(data);
                        }
                    }
                    else if (Common.NowView == ViewEnum.Grid)
                    {
                        Common.Invoke(() =>
                        {
                            if (Common.Main != null)
                            {
                                Common.Main.GridViewCtrl.UpdateThumbnail(data);
                            }
                        });
                    }
                }
                finally
                {
                    processingSet.TryRemove(data.Path, out _);
                }
            })
            {
                IsBackground = true
            }.Start();
        }

        /// <summary>
        /// GetThum 완료 시점에 아직 이 장을 보고 있는지.
        /// NowIndex 만 본다 — ImageArray 없이 이동 시 NowImage 는 디코드 전까지 이전 장일 수 있음.
        /// (NowImage.Path 까지 요구하면 PgDn 등에서 표시가 영구히 취소됨)
        /// </summary>
        private static bool IsStillCurrent(int wantIndex, string wantPath)
        {
            if (NowIndex != wantIndex)
            {
                return false;
            }
            // 경로가 주어졌고 NowImage 가 이미 그 경로로 맞춰져 있으면 OK.
            // NowImage 가 아직 이전 장이어도 NowIndex 가 맞으면 진행 (이동 직후 정상 상태).
            return true;
        }

        /// <summary>현재 스레드에서 동기 디코드 (AllLoad 워커용). 기본 EXIF 생략 (P-1).</summary>
        public static void GetThumNotThread(ImageData data, bool loadExif = false)
        {
            try
            {
                if (data.IsRaw)
                {
                    var thum = RawManager.GetThumbnailArray(data.Path, size, loadExif: loadExif);

                    if (thum.Item1 == null)
                    {
                        MarkDecodeFail(data);
                    }
                    else
                    {
                        data.ImageArray = thum.Item1;
                        if (thum.Item2 != null)
                        {
                            data.Exif = thum.Item2;
                        }
                        MarkDecodeOk(data);
                        AfterImageArrayCached();
                    }
                }
                else if (data.IsBitmap)
                {
                    var bm = BitmapManager.GetBitmapArray(data.Path, size, loadExif: loadExif);
                    if (bm.Item1 == null)
                    {
                        MarkDecodeFail(data);
                    }
                    else
                    {
                        data.ImageArray = bm.Item1;
                        if (bm.Item2 != null)
                        {
                            data.BitmapExif = bm.Item2;
                        }
                        MarkDecodeOk(data);
                        AfterImageArrayCached();
                    }
                }
            }
            finally
            {
                processingSet.TryRemove(data.Path, out _);
                if (Common.NowView == ViewEnum.Grid && Common.Main != null)
                {
                    Common.Invoke(() => Common.Main.GridViewCtrl.UpdateThumbnail(data));
                }
            }
        }

        /// <summary>디코드 실패 기록. MaxDecodeFail 미만이면 재시도 가능, 이상이면 IsNotImage (P-4).</summary>
        private static void MarkDecodeFail(ImageData data)
        {
            if (data == null)
            {
                return;
            }
            data.DecodeFailCount++;
            if (data.DecodeFailCount >= ImageData.MaxDecodeFail)
            {
                data.IsNotImage = true;
                Log.Warn("디코드 영구 실패 " + data.DecodeFailCount + "/" + ImageData.MaxDecodeFail + ": " + data.Path);
            }
            else
            {
                Log.Debug("디코드 일시 실패 " + data.DecodeFailCount + "/" + ImageData.MaxDecodeFail + ": " + data.Path);
            }
        }

        /// <summary>디코드 성공 시 실패 카운트 리셋 (P-4).</summary>
        private static void MarkDecodeOk(ImageData data)
        {
            if (data == null)
            {
                return;
            }
            data.DecodeFailCount = 0;
            data.IsNotImage = false;
        }

        /// <summary>
        /// EXIF 미로드 시 백그라운드 로드 후 현재 장이면 UI 갱신 (P-1).
        /// </summary>
        public static void EnsureExifAsync(ImageData data)
        {
            if (data == null || data.IsNotImage)
            {
                return;
            }
            bool needRaw = data.IsRaw && data.Exif == null;
            bool needBmp = data.IsBitmap && data.BitmapExif == null;
            if (!needRaw && !needBmp)
            {
                return;
            }
            int index = data.Index;
            string path = data.Path;
            Task.Run(() =>
            {
                try
                {
                    if (data.IsRaw && data.Exif == null)
                    {
                        data.Exif = RawManager.GetExifOnly(path);
                        Log.Debug("EXIF 지연 로드 RAW: " + path);
                    }
                    else if (data.IsBitmap && data.BitmapExif == null)
                    {
                        data.BitmapExif = BitmapManager.GetExifOnly(path);
                        Log.Debug("EXIF 지연 로드 BMP: " + path);
                    }
                    if (NowIndex == index && NowImage != null && NowImage.Path == path)
                    {
                        SetExif(data);
                    }
                }
                catch (Exception ex)
                {
                    Log.ExceptionWithMsg(path, ex);
                }
            });
        }

        /// <summary>ImageArray 를 BitmapImage 로 만들어 Single/Full 뷰에 표시. NowIndex/NowImage 갱신.</summary>
        public static void SetImage(ImageData data)
        {
            if (data == null)
            {
                return;
            }
            NowIndex = data.Index;
            Common.Invoke(() =>
            {
                try
                {
                    if (Common.NowView == ViewEnum.Grid)
                    {
                        NowImage = data;
                        Common.Main.GridViewCtrl.UpdateThumbnail(data);
                        Common.Main.GridViewCtrl.Highlight(data);
                    }
                    else if (data.ImageArray != null && data.ImageArray.Length > 0)
                    {
                        BitmapImage bmpImage = new BitmapImage();
                        using (MemoryStream buffer = new MemoryStream(data.ImageArray))
                        {
                            bmpImage.BeginInit();
                            bmpImage.StreamSource = buffer;
                            bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                            bmpImage.EndInit();
                        }

                        switch (Common.NowView)
                        {
                            case ViewEnum.Single:
                                Common.Main.SingleViewCtrl.SetImage(bmpImage);
                                break;
                            case ViewEnum.Full:
                                Common.Main.FullViewCtrl.SetImage(bmpImage);
                                break;
                        }
                        NowImage = data;
                    }
                    else
                    {
                        NowImage = data;
                    }
                }
                catch (Exception ex)
                {
                    Log.ExceptionWithMsg(data.Path, ex);
                }

                SelectedUpdate();

                Common.Main.FocusOut();

            });
        }

        public static void SetExif(ImageData data)
        {
            if (data == null)
            {
                return;
            }

            int selectedCount = 0;
            if (Common.NowSelectorSetting != null)
            {
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                }
            }

            Common.Invoke(() =>
            {
                switch (Common.NowView)
                {
                    case ViewEnum.Grid:
                        break;
                    case ViewEnum.Single:
                        Common.Main.SingleViewCtrl.SetExif(data, selectedCount);
                        break;
                    case ViewEnum.Full:
                        Common.Main.FullViewCtrl.SetExif(data, selectedCount);
                        break;
                    default:
                        break;
                }

            });
        }

        /// <summary>
        /// 이전 이미지로 이동. IsNotImage·선택만 보기 필터 적용.
        /// step&gt;1 (PgUp): 한 번만 step 만큼 점프한 뒤, 부적합 장은 1칸씩 이어서 탐색.
        /// (예전에는 continue 마다 step 을 다시 빼서 PgUp 이 20·30 칸씩 과다 이동함)
        /// </summary>
        public static void PrevImage(int step = 1)
        {
            MoveImage(-1, step);
        }

        /// <summary>
        /// 다음 이미지로 이동. IsNotImage·선택만 보기 필터 적용.
        /// step&gt;1 (PgDn): 한 번만 step 만큼 점프한 뒤, 부적합 장은 1칸씩 이어서 탐색.
        /// </summary>
        public static void NextImage(int step = 1)
        {
            MoveImage(1, step);
        }

        /// <param name="direction">+1 다음, -1 이전</param>
        /// <param name="step">첫 점프 크기 (이후 스킵은 항상 1)</param>
        private static void MoveImage(int direction, int step)
        {
            Common.LastKeydown = DateTime.Now;
            if (direction == 0)
            {
                return;
            }
            if (step < 1)
            {
                step = 1;
            }
            if (ImageList == null || ImageList.Count == 0 || LastIndex < 0)
            {
                return;
            }

            bool firstHop = true;
            int guard = 0;
            int maxGuard = LastIndex + step + 8;

            while (guard++ < maxGuard)
            {
                int delta = firstHop ? step : 1;
                firstHop = false;
                NowIndex += direction * delta;

                if (NowIndex < 0)
                {
                    // 목록 앞으로 넘어감 → 첫 유효 장
                    NowIndex = -1;
                    firstHop = true;
                    step = 1;
                    direction = 1;
                    continue;
                }
                if (NowIndex > LastIndex)
                {
                    // 목록 끝 → 마지막 유효 장
                    NowIndex = LastIndex + 1;
                    firstHop = true;
                    step = 1;
                    direction = -1;
                    continue;
                }

                try
                {
                    ImageData data = ImageList.ElementAt(NowIndex).Value;
                    if (data == null || data.IsNotImage)
                    {
                        continue;
                    }
                    if (!PassesOnlySelectedFilter(data))
                    {
                        continue;
                    }

                    // data.Index 와 NowIndex 동기화 (ElementAt 기준이 진실)
                    data.Index = NowIndex;
                    // 디코드 전에도 현재 장으로 표시 (GetThum IsStillCurrent / 카운트)
                    NowImage = data;

                    // 표시
                    if (data.ImageArray == null || data.ImageArray.Length == 0)
                    {
                        // 카운트/파일명은 즉시, 비트맵은 디코드 후
                        SetExif(data);
                        GetThum(data, true);
                    }
                    else
                    {
                        SetImage(data);
                        SetExif(data);
                        if (App.Setting != null && App.Setting.IsExifVisible)
                        {
                            EnsureExifAsync(data);
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                    break;
                }
            }

            TrimImageArrayCache();
            NearLoad();
        }

        /// <summary>「선택한 사진만」 모드에서 이 장을 보여줄 수 있는지.</summary>
        private static bool PassesOnlySelectedFilter(ImageData data)
        {
            if (!Common.IsOnlySelectedShow || data == null)
            {
                return true;
            }
            if (Common.NowSelectorSetting == null)
            {
                return true;
            }
            lock (Common.NowSelectorSetting.SyncRoot)
            {
                if (Common.NowSelectorSetting.SelectedSet.Count == 0)
                {
                    // 선택 없음: 예전 동작과 같이 이동 중단 위치에서 표시 시도 허용
                    return true;
                }
                return Common.NowSelectorSetting.SelectedSet.Contains(data.FileName);
            }
        }

        public static void FirstImage()
        {
            NowIndex = -1;
            NextImage(1);
        }

        public static void LastImage()
        {
            NowIndex = LastIndex + 1;
            PrevImage(1);
        }

        public static void SelectedUpdate()
        {
            if (IsImageLoaded)
            {
                Task.Run(() => Update());
            }
            else
            {
                Update();
            }

            void Update()
            {
                if (NowDir == null || !File.Exists(settingPath))
                {
                    return;
                }

                if (Common.NowSelectorSetting == null
                    || File.GetLastWriteTime(settingPath) != Common.NowSelectorSettingDate)
                {
                    Common.NowSelectorSetting = SelectorSetting.Load(NowDir);
                    Common.NowSelectorSettingDate = File.GetLastWriteTime(settingPath);
                }

                bool selectedFlg = false;
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    selectedFlg = Common.NowSelectorSetting.SelectedSet.Contains(NowImage.FileName);
                }
                Common.Invoke(() =>
                {
                    switch (Common.NowView)
                    {
                        case ViewEnum.Grid:
                            Common.Main.GridViewCtrl.RefreshSelection();
                            break;
                        case ViewEnum.Single:
                            Common.Main.SingleViewCtrl.SelectedUpdate(selectedFlg);
                            break;
                        case ViewEnum.Full:
                            Common.Main.FullViewCtrl.SelectedUpdate(selectedFlg);
                            break;
                        default:
                            break;
                    }

                });
            }
        }

        public static void UpdateCount()
        {
            if (IsImageLoaded)
            {
                Task.Run(() => SetCount());
            }
            else
            {
                SetCount();
            }

            void SetCount()
            {
                if (NowDir == null || !File.Exists(settingPath))
                {
                    return;
                }

                if (Common.NowSelectorSetting == null)
                {
                    return;
                }

                int selectedCount;
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                }
                Common.Invoke(() =>
                {
                    switch (Common.NowView)
                    {
                        case ViewEnum.Grid:
                            break;
                        case ViewEnum.Single:
                            Common.Main.SingleViewCtrl.SetCount(NowImage.Index, selectedCount);
                            break;
                        case ViewEnum.Full:
                            Common.Main.FullViewCtrl.SetCount(NowImage.Index, selectedCount);
                            break;
                        default:
                            break;
                    }
                });
            }
        }

        /// <summary>현재 폴더 선택을 전부 해제하고 YAML 저장.</summary>
        public static void AllDeselect()
        {
            if (NowDir == null || !File.Exists(settingPath))
            {
                return;
            }

            if (Common.NowSelectorSetting == null)
            {
                return;
            }

            lock (Common.NowSelectorSetting.SyncRoot)
            {
                Common.NowSelectorSetting.SelectedSet.Clear();
            }
            Common.NowSelectorSetting.Save();
            Log.Info($"전체 선택 해제: dir={NowDir}");

            SelectedUpdate();
            UpdateCount();
        }

        /// <summary>
        /// 현재 장 선택 토글 후 동기 저장.
        /// 비동기 fire-and-forget 을 쓰지 않는 이유: 강제 종료 시 선택 유실 방지.
        /// </summary>
        public static void SelectEx()
        {
            if (NowDir == null || !Common.Main.IsWindowReady)
            {
                return;
            }

            if (Common.NowSelectorSetting == null || File.GetLastWriteTime(settingPath) != Common.NowSelectorSettingDate)
            {
                Common.NowSelectorSetting = SelectorSetting.Load(NowDir);
            }

            bool selected;
            int selectedCount;
            lock (Common.NowSelectorSetting.SyncRoot)
            {
                if (Common.NowSelectorSetting.SelectedSet.Contains(NowImage.FileName))
                {
                    Common.NowSelectorSetting.SelectedSet.Remove(NowImage.FileName);
                    selected = false;
                }
                else
                {
                    Common.NowSelectorSetting.SelectedSet.Add(NowImage.FileName);
                    selected = true;
                }
                selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
            }

            try
            {
                Common.NowSelectorSetting.Save();
                Common.NowSelectorSettingDate = File.GetLastWriteTime(settingPath);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            Log.Debug($"선택 토글: file={NowImage?.FileName}, selected={selected}, count={selectedCount}");

            SelectedUpdate();
            UpdateCount();
        }

        public static void UpdateSelectedCount()
        {
            if (Common.NowSelectorSetting != null)
            {
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    var set = Common.NowSelectorSetting.SelectedSet;

                    for (int i = set.Count - 1; i >= 0; i--)
                    {
                        string fileName = set.ElementAt(i);
                        foreach (var item in ImageList.Values)
                        {
                            if (item.FileName == fileName)
                            {
                                goto parentContinue;
                            }
                        }
                        set.Remove(fileName);
                        parentContinue:;
                    }
                }
                Common.NowSelectorSetting.Save();
            }
        }

        public static void ChangeExif(bool? flg = null)
        {
            App.Setting.IsExifVisible = flg != null ? (bool)flg : !App.Setting.IsExifVisible;
            if (App.Setting.IsExifVisible)
            {
                Common.Main.SingleViewCtrl.ExifBd.Visibility = Visibility.Visible;
                Common.Main.SingleViewCtrl.ExifIcon.Opacity = 1;
                Common.Main.FullViewCtrl.ExifBd.Visibility = Visibility.Visible;
                Common.Main.FullViewCtrl.ExifIcon.Opacity = 1;

                SetExif(NowImage);
                EnsureExifAsync(NowImage);
            }
            else
            {
                Common.Main.SingleViewCtrl.ExifBd.Visibility = Visibility.Collapsed;
                Common.Main.SingleViewCtrl.ExifIcon.Opacity = 0.5;
                Common.Main.FullViewCtrl.ExifBd.Visibility = Visibility.Collapsed;
                Common.Main.FullViewCtrl.ExifIcon.Opacity = 0.5;
            }
            App.Setting.Save();
        }
    }
}
