using FastRawSelector.MANAGER;
using FastRawSelector.MODEL;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 그리드 전용 경량 썸네일 디코드.
    /// 싱글용 1616px / GetThumNotThread 를 쓰지 않고, 칸 크기(≤MaxDecodeWidth)로만 뽑아
    /// 디스크 캐시에 저장한다. AllLoad 보다 동시 수를 넉넉히 잡아 스크롤 체감을 살린다.
    /// </summary>
    public static class GridThumbLoader
    {
        /// <summary>그리드 디코드 동시 수 — AllLoad 와 같이 코어의 약 65%.</summary>
        private static int ComputeMaxConcurrent()
        {
            // LoadImage.PrefetchMaxDop 과 동일 정책 (순환 참조 없이 동일 식)
            int n = Environment.ProcessorCount;
            if (n <= 1)
            {
                return 1;
            }
            if (n == 2)
            {
                return 2;
            }
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

        private static readonly int MaxConcurrent = ComputeMaxConcurrent();
        private static readonly SemaphoreSlim Slot = new SemaphoreSlim(MaxConcurrent, MaxConcurrent);

        private static readonly ConcurrentDictionary<string, byte> InFlight =
            new ConcurrentDictionary<string, byte>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 캐시 미스 시 백그라운드 디코드. onReady 는 UI 스레드로 호출.
        /// </summary>
        public static void Request(ImageData data, int decodeWidth, Action<string, BitmapImage> onReady)
        {
            if (data == null || data.IsNotImage || onReady == null)
            {
                return;
            }
            if (string.IsNullOrEmpty(data.Path))
            {
                return;
            }
            if (!data.IsRaw && !data.IsBitmap)
            {
                return;
            }

            decodeWidth = GridThumbCache.GetDecodeWidth(decodeWidth);
            string path = data.Path;

            // 이미 캐시가 있으면 즉시
            try
            {
                var existing = GridThumbCache.LoadThumb(path, data.ImageArray, decodeWidth);
                if (existing != null)
                {
                    onReady(path, existing);
                    return;
                }
            }
            catch
            {
            }

            if (!InFlight.TryAdd(path, 0))
            {
                return;
            }

            int folderGen = LoadImage.FolderGen;
            int dw = decodeWidth;
            bool isRaw = data.IsRaw;
            bool isBitmap = data.IsBitmap;
            // ImageArray 는 대기 중 채워질 수 있어 data 참조 유지
            ImageData dataRef = data;

            Task.Run(() =>
            {
                BitmapImage result = null;
                try
                {
                    Slot.Wait();
                    try
                    {
                        if (folderGen != LoadImage.FolderGen)
                        {
                            return;
                        }

                        byte[] imageArray = dataRef != null ? dataRef.ImageArray : null;
                        result = GridThumbCache.LoadThumb(path, imageArray, dw);
                        if (result != null)
                        {
                            return;
                        }

                        // 경량 디코드: 장변 = 그리드 폭 (1616 아님, EXIF 생략)
                        byte[] jpeg = null;
                        try
                        {
                            if (isRaw)
                            {
                                jpeg = RawManager.GetThumbnailArray(path, dw, loadExif: false).Item1;
                            }
                            else if (isBitmap)
                            {
                                jpeg = BitmapManager.GetBitmapArray(path, dw, loadExif: false).Item1;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.ExceptionWithMsg(path, ex);
                        }

                        if (jpeg == null || jpeg.Length == 0)
                        {
                            return;
                        }

                        result = GridThumbCache.SaveAndLoad(path, jpeg, dw);
                    }
                    finally
                    {
                        Slot.Release();
                    }
                }
                catch (Exception ex)
                {
                    Log.ExceptionWithMsg(path, ex);
                }
                finally
                {
                    byte ignored;
                    InFlight.TryRemove(path, out ignored);
                }

                BitmapImage ready = result;
                Common.Invoke(() =>
                {
                    try
                    {
                        onReady(path, ready);
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                });
            });
        }
    }
}
