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

        private new bool IsLoaded = false;
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
                SelectedCbi.Content = $"선택한 사진만";
                RawPathTb.Focus();
            }
            else
            {
                ExportTargetCb.SelectedIndex = 0;
                AllCbi.IsEnabled = true;
                int selectedCount = 0;
                if (Common.NowSelectorSetting != null)
                {
                    selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                }
                SelectedCbi.Content = $"선택한 사진만 ({selectedCount}장 선택중)";
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

        public void ShowWindow(Window owner)
        {
            Common.MoveCenter(owner, this);
            IsLoaded = false;
            Show();
            Init();
            BringToFront();
            IsLoaded = true;
        }

        private void ExportBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(ExportPathTb.Text))
            {
                WindowAlert("추출할 위치를 입력해주세요.");
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

            int allCount = LoadImage.ImageList.Count;

            Parallel.ForEach(LoadImage.ImageList, (f) =>
            {
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
                    Log.Exception(ex);
                }

                Interlocked.Increment(ref count);
                Common.Invoke(() =>
                {
                    ExportPb.Value = ((double)count / allCount) * 100d;
                });
            });
            WindowAlert("추출완료했습니다.", true);

        }

        public void SelectedExport(string exportPath)
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
                {
                    try
                    {
                        var datas = LoadImage.ImageList.Where(d => d.Key.EndsWith(@"\" + item));

                        ImageData imgData = null;
                        if (datas.Count() > 0)
                        {
                            imgData = datas.ElementAt(0).Value;
                        }

                        if (imgData == null)
                        {
                            continue;
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
                        Console.WriteLine("エラー : " + ex.Message);
                    }
                }
                Interlocked.Increment(ref count);
                Common.Invoke(() =>
                {
                    ExportPb.Value = ((double)count / allCount) * 100d;
                });
            }

            WindowAlert("추출완료했습니다.", true);
        }

        private void SpecifiedFolderExport(string exportPath)
        {
            if (string.IsNullOrEmpty(RawPath) || !Directory.Exists(RawPath))
            {
                WindowAlert("RAW폴더를 입력해주세요.");
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
            int allCount = files.Count;
            Parallel.ForEach(files, (f) =>
            {
                var outJPEG = System.IO.Path.Combine(exportPath, System.IO.Path.GetFileNameWithoutExtension(f) + ".jpg");
                RawManager.RawToJpeg(f, LoadImage.size, outJPEG);

                Interlocked.Increment(ref count);
                if (count % 10 == 0)
                {
                    Common.Invoke(() =>
                    {
                        ExportPb.Value = ((double)count / allCount) * 100d;
                    });
                }
            });
            WindowAlert("추출완료했습니다.", true);
        }
        private void ExportPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder("출력위치", ExportPathTb.Text, f =>
            {
                ExportPathTb.Text = f;
            });
            BringToFront();
        }

        private void ExportTargetCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsLoaded && e.AddedItems != null && e.AddedItems.Count > 0)
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
            Common.GetFolder("RAW폴더", RawPathTb.Text, f =>
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
