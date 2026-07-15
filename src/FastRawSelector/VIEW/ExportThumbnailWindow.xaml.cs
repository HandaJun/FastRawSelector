using FastRawSelector.LOGIC;
using FastRawSelector.MANAGER;
using FastRawSelector.MODEL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// Interaction logic for ExportThumbnailWindow.xaml
    /// </summary>
    public partial class ExportThumbnailWindow : Window
    {

        private bool IsWindowReady = false;
        private string RawPath = null;

        private readonly static ExportThumbnailWindow _instance = new ExportThumbnailWindow();
        public static ExportThumbnailWindow GetInstance()
        {
            return _instance;
        }
        private ExportThumbnailWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            
        }

        public void Init()
        {
            ExportPathTb.Text = LoadImage.NowDir;
            if (LoadImage.NowDir == null)
            {
                AllCbi.IsEnabled = false;
                SelectedCbi.IsEnabled = false;
                ExportTargetCb.SelectedIndex = 2;
                RawPathTb.IsEnabled = true;
                RawPathOpenBt.IsEnabled = true;
                SelectedCbi.Content = Loc.Get("ExportSelected");
                RawPathTb.Focus();
            }
            else
            {
                ExportTargetCb.SelectedIndex = 0;
                AllCbi.IsEnabled = true;
                int selectedCount = 0;
                if (Common.NowSelectorSetting != null)
                {
                    lock (Common.NowSelectorSetting.SyncRoot)
                    {
                        selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                    }
                }
                SelectedCbi.Content = Loc.Get("ExportSelected") + " (" + selectedCount + ")";
                if (selectedCount == 0)
                {
                    SelectedCbi.IsEnabled = false;
                    RawPathTb.Focus();
                }
                else
                {
                    SelectedCbi.IsEnabled = true;
                    ExportTargetCb.SelectedIndex = 1;
                    ExportPathTb.Focus();
                }
            }
        }

        public void ApplyLanguage()
        {
            Title = Loc.Get("ExportTitle");
            if (AllCbi != null)
            {
                AllCbi.Content = Loc.Get("ExportAll");
            }
            if (SpecifiedFolderCbi != null)
            {
                SpecifiedFolderCbi.Content = Loc.Get("ExportSpecified");
            }
        }

        public void ShowWindow(Window owner)
        {
            ApplyLanguage();
            Common.MoveCenter(owner, this);
            IsWindowReady = false;
            Show();
            Init();
            BringToFront();
            IsWindowReady = true;
        }

        private void ExportBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ExportPathTb.Text))
            {
                WindowAlert(Loc.Get("ExportNeedPath"));
                return;
            }

            string exportPath = ExportPathTb.Text;
            if (!Directory.Exists(exportPath))
            {
                Directory.CreateDirectory(exportPath);
            }

            if (ExportTargetCb.SelectedItem is ComboBoxItem cbi)
            {
                ExportPb.Visibility = Visibility.Visible;
                string Kind = cbi.Tag.ToString();
                RawPath = RawPathTb.Text;
                Log.Info($"섬네일 추출 시작: kind={Kind}, path={exportPath}");
                Task.Run(() =>
                  {
                      switch (Kind)
                      {
                          case "All":
                              AllExport(exportPath);
                              ExportPathTb.Focus();
                              break;
                          case "Selected":
                              SelectedExport(exportPath);
                              ExportPathTb.Focus();
                              break;
                          case "SpecifiedFolder":
                              SpecifiedFolderExport(exportPath);
                              RawPathTb.Focus();
                              break;
                          default:
                              break;
                      }
                  });
            }
        }

        public void AllExport(string exportPath)
        {
            int count = 0;
            int failCount = 0;
            // Part 2: 목록 스냅샷 + 폴더 세대 + DOP 상한 (AllLoad 와 동일 패턴)
            int folderGen = LoadImage.FolderGen;
            int dop = LoadImage.PrefetchMaxDop;
            var snapshot = LoadImage.ImageList.ToList();
            int allCount = snapshot.Count;
            Log.Info($"섬네일 전체 추출: target={allCount}, maxDop={dop}, path={exportPath}");

            var opts = new ParallelOptions { MaxDegreeOfParallelism = dop };
            try
            {
                Parallel.ForEach(snapshot, opts, (f, state) =>
                {
                    if (folderGen != LoadImage.FolderGen)
                    {
                        state.Stop();
                        return;
                    }
                    try
                    {
                        var outJPEG = System.IO.Path.Combine(exportPath, System.IO.Path.GetFileNameWithoutExtension(f.Value.Path) + ".jpg");
                        if (!f.Value.IsNotImage && f.Value.ImageArray == null)
                        {
                            RawManager.RawToJpeg(f.Value.Path, LoadImage.size, outJPEG);
                        }
                        else if (f.Value.ImageArray != null)
                        {
                            using (var ms = new MemoryStream(f.Value.ImageArray))
                            {
                                using (var fs = new FileStream(outJPEG, FileMode.Create))
                                {
                                    ms.WriteTo(fs);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failCount);
                        Log.Exception(ex);
                    }

                    Interlocked.Increment(ref count);
                    if (allCount > 0)
                    {
                        Common.Invoke(() =>
                        {
                            try
                            {
                                ExportPb.Value = ((double)count / allCount) * 100d;
                            }
                            catch (Exception uiEx)
                            {
                                Log.Exception(uiEx);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            if (folderGen != LoadImage.FolderGen)
            {
                Log.Info($"섬네일 전체 추출 취소(폴더 전환): done={count}/{allCount}, fail={failCount}");
                Common.Invoke(() => { ExportPb.Visibility = Visibility.Collapsed; });
                return;
            }

            Log.Info($"섬네일 전체 추출 완료: total={allCount}, fail={failCount}, path={exportPath}");
            WindowAlert(Loc.Get("ExportDone"), true);
        }

        public void SelectedExport(string exportPath)
        {
            if (Common.NowSelectorSetting == null)
            {
                WindowAlert(Loc.Get("ExportNoSelected"));
                return;
            }

            List<string> selectedItems;
            lock (Common.NowSelectorSetting.SyncRoot)
            {
                if (Common.NowSelectorSetting.SelectedSet.Count == 0)
                {
                    WindowAlert(Loc.Get("ExportNoSelected"));
                    return;
                }
                selectedItems = Common.NowSelectorSetting.SelectedSet.ToList();
            }

            int count = 0;
            int failCount = 0;
            int allCount = selectedItems.Count;
            int folderGen = LoadImage.FolderGen;
            int dop = LoadImage.PrefetchMaxDop;
            // 선택 목록 스냅샷 후 Parallel (Part 2)
            var work = new List<ImageData>();
            foreach (var item in selectedItems)
            {
                var datas = LoadImage.ImageList.Where(d => d.Key.EndsWith(@"\" + item)).ToList();
                if (datas.Count > 0)
                {
                    work.Add(datas[0].Value);
                }
            }
            allCount = work.Count;
            Log.Info($"섬네일 선택 추출: target={allCount}, maxDop={dop}, path={exportPath}");

            var opts = new ParallelOptions { MaxDegreeOfParallelism = dop };
            try
            {
                Parallel.ForEach(work, opts, (imgData, state) =>
                {
                    if (folderGen != LoadImage.FolderGen)
                    {
                        state.Stop();
                        return;
                    }
                    try
                    {
                        if (imgData == null)
                        {
                            return;
                        }
                        var outJPEG = System.IO.Path.Combine(exportPath, System.IO.Path.GetFileNameWithoutExtension(imgData.Path) + ".jpg");
                        if (!imgData.IsNotImage && imgData.ImageArray == null)
                        {
                            RawManager.RawToJpeg(imgData.Path, LoadImage.size, outJPEG);
                        }
                        else if (imgData.ImageArray != null)
                        {
                            using (var ms = new MemoryStream(imgData.ImageArray))
                            {
                                using (var fs = new FileStream(outJPEG, FileMode.Create))
                                {
                                    ms.WriteTo(fs);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failCount);
                        Log.Exception(ex);
                    }
                    Interlocked.Increment(ref count);
                    if (allCount > 0)
                    {
                        Common.Invoke(() =>
                        {
                            try
                            {
                                ExportPb.Value = ((double)count / allCount) * 100d;
                            }
                            catch (Exception uiEx)
                            {
                                Log.Exception(uiEx);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }

            if (folderGen != LoadImage.FolderGen)
            {
                Log.Info($"섬네일 선택 추출 취소(폴더 전환): done={count}/{allCount}, fail={failCount}");
                Common.Invoke(() => { ExportPb.Visibility = Visibility.Collapsed; });
                return;
            }

            Log.Info($"섬네일 선택 추출 완료: total={allCount}, fail={failCount}, path={exportPath}");
            WindowAlert(Loc.Get("ExportDone"), true);
        }

        private void SpecifiedFolderExport(string exportPath)
        {
            if (string.IsNullOrEmpty(RawPath) || !Directory.Exists(RawPath))
            {
                WindowAlert(Loc.Get("ExportNeedRaw"));
                return;
            }

            List<string> files = Directory.GetFiles(RawPath, "*", SearchOption.TopDirectoryOnly).ToList();
            for (int i = files.Count - 1; i >= 0; i--)
            {
                string file = files.ElementAt(i);
                if (!Common.IsRawFile(file))
                {
                    files.RemoveAt(i);
                }
            }

            int count = 0;
            int failCount = 0;
            int allCount = files.Count;
            // 지정 폴더는 ImageList 와 무관 — 작업 세대 토큰으로 창 닫기/재시작 대응은 약함.
            // DOP 만 AllLoad 와 맞춤 (Part 2).
            int dop = LoadImage.PrefetchMaxDop;
            Log.Info($"섬네일 지정폴더 추출: target={allCount}, maxDop={dop}, raw={RawPath}, path={exportPath}");
            var opts = new ParallelOptions { MaxDegreeOfParallelism = dop };
            try
            {
                Parallel.ForEach(files, opts, (f) =>
                {
                    try
                    {
                        var outJPEG = System.IO.Path.Combine(exportPath, System.IO.Path.GetFileNameWithoutExtension(f) + ".jpg");
                        RawManager.RawToJpeg(f, LoadImage.size, outJPEG);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failCount);
                        Log.Exception(ex);
                    }

                    Interlocked.Increment(ref count);
                    if (count % 10 == 0 && allCount > 0)
                    {
                        Common.Invoke(() =>
                        {
                            try
                            {
                                ExportPb.Value = ((double)count / allCount) * 100d;
                            }
                            catch (Exception uiEx)
                            {
                                Log.Exception(uiEx);
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            Log.Info($"섬네일 지정폴더 추출 완료: total={allCount}, fail={failCount}, path={exportPath}");
            WindowAlert(Loc.Get("ExportDone"), true);
        }
        private void ExportPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerOutput"), ExportPathTb.Text, f =>
            {
                ExportPathTb.Text = f;
            });
            BringToFront();
        }

        private void ExportTargetCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsWindowReady && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is ComboBoxItem cbi)
                {
                    switch (cbi.Tag.ToString())
                    {
                        case "All":
                        case "Selected":
                            RawPathTb.IsEnabled = false;
                            RawPathOpenBt.IsEnabled = false;
                            break;
                        case "SpecifiedFolder":
                            RawPathTb.IsEnabled = true;
                            RawPathOpenBt.IsEnabled = true;
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void RawPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerRaw"), RawPathTb.Text, f =>
            {
                RawPathTb.Text = f;
                if (string.IsNullOrEmpty(ExportPathTb.Text))
                {
                    ExportPathTb.Text = f;
                }
            });
            BringToFront();
        }

        private void WindowAlert(string msg, bool closeFlg = false)
        {
            Common.Invoke(() =>
            {
                ExportPb.Visibility = Visibility.Collapsed;
                _ = Alert.Info(msg, afterAct: () =>
                {
                    if (closeFlg)
                    {
                        Close();
                    }
                    else
                    {
                        BringToFront();
                    }
                });
            });
        }

        public void BringToFront()
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.ToolTip = tb.Text;
            }
        }

        private void TitleGrid_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            {
                try
                {
                    DragMove();
                }
                catch (Exception)
                {
                }
            }
        }

        private void CloseBt_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }
    }
}
