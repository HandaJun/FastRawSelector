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
    public class LoadImage
    {

        public static Dictionary<string, ImageData> ImageList = new Dictionary<string, ImageData>();
        public static ImageData NowImage = null;
        public static int NowIndex = -1;
        public static int LastIndex = -1;
        public static int size = 1616;
        public static ConcurrentDictionary<string, string> processingSet = new ConcurrentDictionary<string, string>();
        public static string NowDir = null;
        public static string settingPath = null;
        public static bool IsImageLoaded = false;

        public static void SetArg(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            IsImageLoaded = false;
            Common.Main.IsLoaded = false;
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
            SortedDictionary<string, ImageData> tempDic = new SortedDictionary<string, ImageData>();

            for (int i = files.Count - 1; i >= 0; i--)
            {
                string file = files.ElementAt(i);

                if (Common.IsRawFile(file))
                {
                    tempDic.Add(file,
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
                    tempDic.Add(file,
                            new ImageData()
                            {
                                Path = file,
                                FileName = System.IO.Path.GetFileName(file),
                                IsBitmap = true
                            }
                        );
                }
                else
                {
                    files.RemoveAt(i);
                }
            }

            ImageList = new Dictionary<string, ImageData>(tempDic);

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
                    if (NowImage.IsRaw)
                    {
                        var thum = RawManager.GetThumbnailArray(path, size);
                        NowImage.ImageArray = thum.Item1;
                        NowImage.Exif = thum.Item2;
                    }
                    else if (NowImage.IsBitmap)
                    {
                        var thum = BitmapManager.GetBitmapArray(path, size);
                        NowImage.ImageArray = thum.Item1;
                        NowImage.BitmapExif = thum.Item2;
                    }

                    SetImage(NowImage);
                }
            }

            LastIndex = ImageList.Count - 1;

            SetExif(NowImage);

            ChangeExif(App.Setting.IsExifVisible);

            Common.Main.ShowViewControl();

            NearLoad(AllLoad);

            Common.Main.IsLoaded = true;

        }

        public static void NearLoad(Action afterAct = null)
        {
            Thread t = new Thread(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int index = NowIndex + (j == 0 ? i * 1 : i * -1);
                        if (index < 0 || index > LastIndex)
                        {
                            continue;
                        }
                        ImageData data = ImageList.ElementAt(i).Value;
                        if (processingSet.ContainsKey(data.Path))
                        {
                            continue;
                        }
                        processingSet.TryAdd(data.Path, null);

                        if (!data.IsNotImage && data.ImageArray == null)
                        {
                            GetThum(data, false);
                        }
                        else
                        {
                            processingSet.TryRemove(data.Path, out _);
                        }
                    }
                }

                afterAct?.Invoke();
            });
            t.Priority = ThreadPriority.Highest;
            t.Start();
        }

        public static void AllLoad()
        {
            Task.Run(() =>
            {
                int count = 0;
                Parallel.ForEach(ImageList, (f) =>
                {
                    try
                    {
                        if (!processingSet.ContainsKey(f.Key))
                        {
                            processingSet.TryAdd(f.Key, null);
                            if (!f.Value.IsNotImage && f.Value.ImageArray == null)
                            {
                                GetThumNotThread(f.Value);
                            }
                        }
                        Interlocked.Increment(ref count);
                        if (count % 10 == 0)
                        {
                            Common.Invoke(() =>
                            {
                                try
                                {
                                    Common.Main.LoadingPb.Value = ((double)count / LastIndex) * 100d;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Common.Invoke(() => { 
                            MessageBox.Show(ex.Message + "\n" + f.Key);
                        });
                    }
                });
                Common.Invoke(() =>
                {
                    Common.Main.LoadingGrid.Visibility = Visibility.Collapsed;
                    IsImageLoaded = true;
                });
            });
        }
        public static void GetThum(ImageData data, bool IsSetImage)
        {
            new Thread(() =>
            {
                Thread.Sleep(10);
                if (IsSetImage && NowIndex != data.Index)
                {
                    return;
                }
                if (data.IsRaw)
                {
                    var thum = RawManager.GetThumbnailArray(data.Path, size);
                    if (thum.Item1 == null)
                    {
                        data.IsNotImage = true;
                    }
                    else
                    {
                        data.ImageArray = thum.Item1;
                        data.Exif = thum.Item2;
                    }
                }
                else if (data.IsBitmap)
                {
                    var bm = BitmapManager.GetBitmapArray(data.Path, size);
                    if (bm.Item1 == null)
                    {
                        data.IsNotImage = true;
                    }
                    else
                    {
                        data.ImageArray = bm.Item1;
                        data.BitmapExif = bm.Item2;
                    }
                }

                if (IsSetImage && NowIndex == data.Index)
                {
                    SetImage(data);
                    SetExif(data);
                }

                processingSet.TryRemove(data.Path, out _);
            }).Start();
        }

        public static void GetThumNotThread(ImageData data)
        {
            if (data.IsRaw)
            {
                var thum = RawManager.GetThumbnailArray(data.Path, size);

                if (thum.Item1 == null)
                {
                    data.IsNotImage = true;
                }
                else
                {
                    data.ImageArray = thum.Item1;
                    data.Exif = thum.Item2;
                }
            }
            else if (data.IsBitmap)
            {
                var bm = BitmapManager.GetBitmapArray(data.Path, size);
                if (bm.Item1 == null)
                {
                    data.IsNotImage = true;
                }
                else
                {
                    data.ImageArray = bm.Item1;
                    data.BitmapExif = bm.Item2;
                }
            }
            processingSet.TryRemove(data.Path, out _);
        }

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
                    BitmapImage bmpImage = new BitmapImage();
                    using (MemoryStream buffer = new MemoryStream(data.ImageArray))
                    {
                        if (buffer.Length == 0)
                        {
                            return;
                        }

                        bmpImage.BeginInit();
                        bmpImage.StreamSource = buffer;
                        bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmpImage.EndInit();
                    }

                    switch (Common.NowView)
                    {
                        case ViewEnum.Grid:
                            break;
                        case ViewEnum.Single:
                            Common.Main.SingleViewCtrl.SetImage(bmpImage);
                            break;
                        case ViewEnum.Full:
                            Common.Main.FullViewCtrl.SetImage(bmpImage);
                            break;
                        default:
                            break;
                    }
                    NowImage = data;
                }
                catch (Exception)
                {
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
                selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
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

        public static void PrevImage(int step = 1)
        {
            Common.LastKeydown = DateTime.Now;
            while (true)
            {
                NowIndex -= step;
                if (NowIndex < 0)
                {
                    NowIndex = -1;
                    NextIamge();
                    break;
                }
                ImageData data = ImageList.ElementAt(NowIndex).Value;
                if (data.IsNotImage)
                {
                    break;
                }
                if (data.ImageArray == null)
                {
                    GetThum(data, true);
                    break;
                }
                else
                {
                    SetImage(data);
                    SetExif(data);
                    break;
                }
            }
            NearLoad();
        }

        public static void NextIamge(int step = 1)
        {
            Common.LastKeydown = DateTime.Now;
            while (true)
            {
                NowIndex += step;
                if (NowIndex > LastIndex)
                {
                    NowIndex = LastIndex + 1;
                    PrevImage();
                    break;
                }
                ImageData data = ImageList.ElementAt(NowIndex).Value;
                if (data.IsNotImage)
                {
                    break;
                }
                if (data.ImageArray == null)
                {
                    GetThum(data, true);
                    break;
                }
                else
                {
                    SetImage(data);
                    SetExif(data);
                    break;
                }
            }
            NearLoad();
        }

        public static void FirstImage()
        {
            NowIndex = -1;
            NextIamge();
        }

        public static void LastImage()
        {
            NowIndex = LastIndex + 1;
            PrevImage();
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
                if (Common.NowSelectorSetting.SelectedSet.Contains(NowImage.FileName))
                {
                    selectedFlg = true;
                }
                Common.Invoke(() =>
                {
                    switch (Common.NowView)
                    {
                        case ViewEnum.Grid:
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

                int selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
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

            Common.NowSelectorSetting.SelectedSet.Clear();
            Common.NowSelectorSetting.Save();

            SelectedUpdate();
            UpdateCount();
        }

        public static void SelectEx()
        {
            if (NowDir == null || !Common.Main.IsLoaded)
            {
                return;
            }

            if (Common.NowSelectorSetting == null || File.GetLastWriteTime(settingPath) != Common.NowSelectorSettingDate)
            {
                Common.NowSelectorSetting = SelectorSetting.Load(NowDir);
            }

            _ = Common.NowSelectorSetting.SelectedSet.Contains(NowImage.FileName)
                ? Common.NowSelectorSetting.SelectedSet.Remove(NowImage.FileName)
                : Common.NowSelectorSetting.SelectedSet.Add(NowImage.FileName);

            Common.NowSelectorSettingDate = File.GetLastWriteTime(settingPath);
            SelectedUpdate();
            UpdateCount();
            Task.Run(() =>
            {
                try
                {
                    Common.NowSelectorSetting.Save();
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            });
        }

        public static void UpdateSelectedCount()
        {
            if (Common.NowSelectorSetting != null)
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

                if (flg == null)
                {
                    SetExif(NowImage);
                }
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
