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
        private new bool IsLoaded = false;
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
            IsLoaded = true;
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
                SelectedCbi.Content = $"선택한 사진";
                RawPathTb.Focus();
            }
            else
            {
                CopyTargetCb.SelectedIndex = 0;
                int selectedCount = 0;
                if (Common.NowSelectorSetting != null)
                {
                    selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                }
                SelectedCbi.Content = $"선택한 사진 ({selectedCount}장 선택중)";
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
            if (IsLoaded && e.AddedItems != null && e.AddedItems.Count > 0)
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
            Common.GetFolder("JPEG폴더", JpegPathTb.Text, f =>
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
            Common.GetFolder("RAW폴더", RawPathTb.Text, f =>
            {
                RawPathTb.Text = f;
            });
            BringToFront();
        }

        private void CopyPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder("복사위치", CopyPathTb.Text, f =>
            {
                CopyPathTb.Text = f;
            });
            BringToFront();
        }

        private void CopyBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(CopyPathTb.Text))
            {
                WindowAlert("복사할 위치를 입력해주세요.");
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
            if (Common.NowSelectorSetting == null && Common.NowSelectorSetting.SelectedSet.Count == 0)
            {
                WindowAlert("선택한 사진이 없습니다.");
                return;
            }

            int count = 0;
            int allCount = Common.NowSelectorSetting.SelectedSet.Count;

            foreach (var item in Common.NowSelectorSetting.SelectedSet)
            {
                try
                {
                    string sourcePath = Path.Combine(LoadImage.NowDir, item);
                    string targetPath = Path.Combine(copyPath, item);
                    if (!File.Exists(sourcePath))
                    {
                        throw new Exception("not file " + sourcePath);
                    }
                    File.Copy(sourcePath, targetPath, true);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー : " + ex.Message);
                }
                Interlocked.Increment(ref count);
                Common.Invoke(() =>
                {
                    CopyPb.Value = ((double)count / allCount) * 100d;
                });
            }
            WindowAlert("복사완료했습니다.", true);
        }

        private void SpecifiedFolderCopy(string copyPath)
        {
            if (string.IsNullOrEmpty(RawPath) || !Directory.Exists(RawPath))
            {
                WindowAlert("RAW폴더를 입력해주세요.");
                return;
            }
            if (string.IsNullOrEmpty(JpegPath) || !Directory.Exists(JpegPath))
            {
                WindowAlert("JPEG폴더를 입력해주세요.");
                return;
            }

            try
            {
                List<string> jpegFiles = Directory.GetFiles(JpegPath, "*", SearchOption.TopDirectoryOnly).ToList();
                List<string> rawFiles = Directory.GetFiles(RawPath, "*", SearchOption.TopDirectoryOnly).ToList();

                int count = 0;
                int allCount = jpegFiles.Count;

                foreach (var jpegFullPath in jpegFiles)
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
                        Console.WriteLine(ex.Message);
                    }
                    Interlocked.Increment(ref count);
                    Common.Invoke(() =>
                    {
                        CopyPb.Value = ((double)count / allCount) * 100d;
                    });

                }
                WindowAlert("복사완료했습니다.", true);
            }
            catch (Exception)
            {
                WindowAlert("복사실패했습니다.", true);
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
