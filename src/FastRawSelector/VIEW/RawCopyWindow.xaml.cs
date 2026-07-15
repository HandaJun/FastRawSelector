using FastRawSelector.LOGIC;
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
    /// Interaction logic for RawCopyWindow.xaml
    /// </summary>
    public partial class RawCopyWindow : Window
    {
        private bool IsWindowReady = false;
        private string RawPath = null;
        private string JpegPath = null;

        private readonly static RawCopyWindow _instance = new RawCopyWindow();
        public static RawCopyWindow GetInstance()
        {
            return _instance;
        }
        private RawCopyWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            RawPathTb.Text = LoadImage.NowDir;
            if (LoadImage.NowDir == null)
            {
                SelectedCbi.IsEnabled = false;
                CopyTargetCb.SelectedIndex = 1;
                JpegPathTb.IsEnabled = true;
                JpegPathOpenBt.IsEnabled = true;

            }
            IsWindowReady = true;
        }

        public void ShowWindow(Window owner)
        {
            Common.MoveCenter(owner, this);
            Show();
            Init();
            BringToFront();
        }

        public void Init()
        {
            RawPathTb.Text = LoadImage.NowDir;
            if (LoadImage.NowDir == null)
            {
                SelectedCbi.IsEnabled = false;
                CopyTargetCb.SelectedIndex = 1;
                JpegPathTb.IsEnabled = true;
                JpegPathOpenBt.IsEnabled = true;
                SelectedCbi.Content = Loc.Get("CopySelected");
                RawPathTb.Focus();
            }
            else
            {
                CopyTargetCb.SelectedIndex = 0;
                int selectedCount = 0;
                if (Common.NowSelectorSetting != null)
                {
                    lock (Common.NowSelectorSetting.SyncRoot)
                    {
                        selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                    }
                }
                SelectedCbi.Content = Loc.Get("CopySelected") + " (" + selectedCount + ")";
                if (selectedCount == 0)
                {
                    SelectedCbi.IsEnabled = false;
                    RawPathTb.Focus();
                }
                else
                {
                    SelectedCbi.IsEnabled = true;
                    CopyTargetCb.SelectedIndex = 0;
                    CopyPathTb.Focus();
                }
            }
        }
        private void CopyTargetCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsWindowReady && e.AddedItems != null && e.AddedItems.Count > 0)
            {
                if (e.AddedItems[0] is ComboBoxItem cbi)
                {
                    switch (cbi.Tag.ToString())
                    {
                        case "Selected":
                            JpegPathTb.IsEnabled = false;
                            JpegPathOpenBt.IsEnabled = false;
                            CopyPathTb.Focus();
                            break;
                        case "SpecifiedFolder":
                            JpegPathTb.IsEnabled = true;
                            JpegPathOpenBt.IsEnabled = true;
                            RawPathTb.Focus();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void JpegPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerJpeg"), JpegPathTb.Text, f =>
            {
                JpegPathTb.Text = f;
                if (string.IsNullOrEmpty(CopyPathTb.Text))
                {
                    CopyPathTb.Text = f;
                }
            });
            BringToFront();
        }

        private void RawPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerRaw"), RawPathTb.Text, f =>
            {
                RawPathTb.Text = f;
            });
            BringToFront();
        }

        private void CopyPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerCopy"), CopyPathTb.Text, f =>
            {
                CopyPathTb.Text = f;
            });
            BringToFront();
        }

        private void CopyBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CopyPathTb.Text))
            {
                WindowAlert(Loc.Get("CopyNeedPath"));
                return;
            }

            string copyPath = CopyPathTb.Text;
            if (!Directory.Exists(copyPath))
            {
                Directory.CreateDirectory(copyPath);
            }

            if (CopyTargetCb.SelectedItem is ComboBoxItem cbi)
            {
                CopyPb.Visibility = Visibility.Visible;
                string Kind = cbi.Tag.ToString();

                RawPath = RawPathTb.Text;
                JpegPath = JpegPathTb.Text;
                Log.Info($"RAW 복사 시작: kind={Kind}, path={copyPath}");
                Task.Run(() =>
                {
                    switch (Kind)
                    {
                        case "Selected":
                            SelectedCopy(copyPath);
                            break;
                        case "SpecifiedFolder":
                            SpecifiedFolderCopy(copyPath);
                            break;
                        default:
                            break;
                    }
                });
            }
        }

        public void SelectedCopy(string copyPath)
        {
            if (Common.NowSelectorSetting == null)
            {
                WindowAlert(Loc.Get("CopyNoSelected"));
                return;
            }

            List<string> selectedItems;
            lock (Common.NowSelectorSetting.SyncRoot)
            {
                if (Common.NowSelectorSetting.SelectedSet.Count == 0)
                {
                    WindowAlert(Loc.Get("CopyNoSelected"));
                    return;
                }
                selectedItems = Common.NowSelectorSetting.SelectedSet.ToList();
            }

            int count = 0;
            int failCount = 0;
            int allCount = selectedItems.Count;
            // Part 2: 폴더 세대 + 경로 스냅샷 (폴더 전환 중 잘못된 경로 복사 방지)
            int folderGen = LoadImage.FolderGen;
            string sourceDir = LoadImage.NowDir;
            Log.Info($"RAW 선택 복사: target={allCount}, path={copyPath}");

            foreach (var item in selectedItems)
            {
                if (folderGen != LoadImage.FolderGen)
                {
                    Log.Info($"RAW 선택 복사 취소(폴더 전환): done={count}/{allCount}, fail={failCount}");
                    Common.Invoke(() => { CopyPb.Visibility = Visibility.Collapsed; });
                    return;
                }
                try
                {
                    string sourcePath = Path.Combine(sourceDir ?? "", item);
                    string targetPath = Path.Combine(copyPath, item);
                    if (!File.Exists(sourcePath))
                    {
                        throw new Exception("not file " + sourcePath);
                    }
                    File.Copy(sourcePath, targetPath, true);
                }
                catch (Exception ex)
                {
                    failCount++;
                    Log.Exception(ex);
                }
                Interlocked.Increment(ref count);
                if (allCount > 0)
                {
                    Common.Invoke(() =>
                    {
                        try
                        {
                            CopyPb.Value = ((double)count / allCount) * 100d;
                        }
                        catch (Exception uiEx)
                        {
                            Log.Exception(uiEx);
                        }
                    });
                }
            }
            Log.Info($"RAW 선택 복사 완료: total={allCount}, fail={failCount}, path={copyPath}");
            WindowAlert(Loc.Get("CopyDone"), true);
        }

        private void SpecifiedFolderCopy(string copyPath)
        {
            if (string.IsNullOrEmpty(RawPath) || !Directory.Exists(RawPath))
            {
                WindowAlert(Loc.Get("CopyNeedRaw"));
                return;
            }
            if (string.IsNullOrEmpty(JpegPath) || !Directory.Exists(JpegPath))
            {
                WindowAlert(Loc.Get("CopyNeedJpeg"));
                return;
            }

            try
            {
                List<string> jpegFiles = Directory.GetFiles(JpegPath, "*", SearchOption.TopDirectoryOnly).ToList();
                List<string> rawFiles = Directory.GetFiles(RawPath, "*", SearchOption.TopDirectoryOnly).ToList();

                int count = 0;
                int failCount = 0;
                int allCount = jpegFiles.Count;
                int dop = LoadImage.PrefetchMaxDop;
                Log.Info($"RAW 지정폴더 복사: jpeg={allCount}, maxDop={dop}, raw={RawPath}, path={copyPath}");

                // 파일 I/O 병렬 (Part 2). 지정 폴더는 ImageList 세대와 무관.
                var opts = new ParallelOptions { MaxDegreeOfParallelism = dop };
                Parallel.ForEach(jpegFiles, opts, (jpegFullPath) =>
                {
                    try
                    {
                        string jpegFileName = Path.GetFileNameWithoutExtension(jpegFullPath);
                        var sourceFiles = rawFiles.Where(f => Path.GetFileNameWithoutExtension(f) == jpegFileName);
                        foreach (var sourcePath in sourceFiles)
                        {
                            string targetPath = Path.Combine(copyPath, Path.GetFileName(sourcePath));
                            File.Copy(sourcePath, targetPath, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failCount);
                        Log.Exception(ex);
                    }
                    Interlocked.Increment(ref count);
                    if (allCount > 0 && count % 5 == 0)
                    {
                        Common.Invoke(() =>
                        {
                            try
                            {
                                CopyPb.Value = ((double)count / allCount) * 100d;
                            }
                            catch (Exception uiEx)
                            {
                                Log.Exception(uiEx);
                            }
                        });
                    }
                });
                Log.Info($"RAW 지정폴더 복사 완료: total={allCount}, fail={failCount}, path={copyPath}");
                WindowAlert(Loc.Get("CopyDone"), true);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                WindowAlert(Loc.Get("CopyFail"), true);
            }
        }

        private void WindowAlert(string msg, bool closeFlg = false)
        {
            Common.Invoke(() =>
            {
                CopyPb.Visibility = Visibility.Collapsed;
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
