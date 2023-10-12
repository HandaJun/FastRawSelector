using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using FastRawSelector.VIEW;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace FastRawSelector
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Args = null;
        public new bool IsLoaded = false;

        bool IsFullScreen = false;
        private WindowState agoWindowState = WindowState.Maximized;
        string OriginalTitle = "";

        public MainWindow(string[] args)
        {
            InitializeComponent();
            this.Args = args;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Common.Main = this;
            ((Storyboard)FindResource("WaitStoryboard")).Begin();

            OriginalTitle = Title + Common.GetVersion(Application.ResourceAssembly, " Beta ", 3);
            Title = OriginalTitle;

            LoadingGrid.Visibility = Visibility.Collapsed;
            if (Args.Length != 0)
            {
                LoadImage.SetArg(Args[0]);
            }

            SingleViewCtrl.MainImg.Focus();
            IsLoaded = true;
        }

        private void ExportThumbnailBt_Click(object sender, RoutedEventArgs e)
        {
            ExportThumbnailWindow.GetInstance().ShowWindow(this);
        }

        private void RawCopyBt_Click(object sender, RoutedEventArgs e)
        {
            RawCopyWindow.GetInstance().ShowWindow(this);
        }

        private void ExitBt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void GridViewBt_Click(object sender, RoutedEventArgs e)
        {
            ViewChange(ViewEnum.Grid);
        }

        private void SingleViewBt_Click(object sender, RoutedEventArgs e)
        {
            if (Common.NowView == ViewEnum.Single)
            {
                return;
            }
            FullScreen(false, ViewEnum.Single);
        }

        private void FullViewBt_Click(object sender, RoutedEventArgs e)
        {
            if (Common.NowView == ViewEnum.Full)
            {
                return;
            }
            FullScreen(true);
        }

        public void ShowViewControl()
        {
            ViewChange(Common.NowView);
        }
        public void ViewChange(ViewEnum viewEnum)
        {
            Common.AgoView = Common.NowView;
            Common.NowView = viewEnum;
            switch (viewEnum)
            {
                case ViewEnum.Grid:
                    GridViewIcon.Foreground = Common.MainBrush;
                    SingleViewIcon.Foreground = Common.GrayBrush;
                    FullViewIcon.Foreground = Common.GrayBrush;
                    break;
                case ViewEnum.Single:
                    SingleViewIcon.Foreground = Common.MainBrush;
                    GridViewIcon.Foreground = Common.GrayBrush;
                    FullViewIcon.Foreground = Common.GrayBrush;
                    break;
                case ViewEnum.Full:
                    FullViewIcon.Foreground = Common.MainBrush;
                    SingleViewIcon.Foreground = Common.GrayBrush;
                    GridViewIcon.Foreground = Common.GrayBrush;
                    break;
                default:
                    break;
            }

            if (LoadImage.NowImage != null)
            {
                switch (viewEnum)
                {
                    case ViewEnum.Grid:
                        GridViewCtrl.Visibility = Visibility.Visible;
                        SingleViewCtrl.Visibility = Visibility.Collapsed;
                        FullViewCtrl.Visibility = Visibility.Collapsed;

                        break;
                    case ViewEnum.Single:
                        SingleViewCtrl.Visibility = Visibility.Visible;
                        GridViewCtrl.Visibility = Visibility.Collapsed;
                        FullViewCtrl.Visibility = Visibility.Collapsed;
                        SingleViewCtrl.MainImg.Focus();
                        break;
                    case ViewEnum.Full:
                        SingleViewCtrl.Visibility = Visibility.Collapsed;
                        GridViewCtrl.Visibility = Visibility.Collapsed;
                        FullViewCtrl.Visibility = Visibility.Visible;
                        FullViewCtrl.MainImg.Focus();
                        break;
                    default:
                        break;
                }
                LoadImage.SetImage(LoadImage.NowImage);
                LoadImage.SetExif(LoadImage.NowImage);
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            switch (Common.NowView)
            {
                case ViewEnum.Grid:
                    break;
                case ViewEnum.Single:
                case ViewEnum.Full:
                    if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    {
                        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                        LoadingGrid.Visibility = Visibility.Visible;
                        LoadImage.SetArg(files[0]);
                    }
                    break;
                default:
                    break;
            }

        }


        public void FullScreen(bool? flg = null, ViewEnum? ve = null)
        {
            if (flg != null)
            {
                IsFullScreen = (bool)flg;
            }
            else
            {
                IsFullScreen = !IsFullScreen;
            }

            if (IsFullScreen)
            {
                WindowStyle = WindowStyle.None;
                agoWindowState = WindowState;
                WindowState = WindowState.Normal;
                WindowState = WindowState.Maximized;
                MenuBar.Visibility = Visibility.Collapsed;
                ViewChange(ViewEnum.Full);
            }
            else
            {
                WindowState = agoWindowState;
                WindowStyle = WindowStyle.SingleBorderWindow;
                MenuBar.Visibility = Visibility.Visible;
                ViewChange(ve == null ? ViewEnum.Single : (ViewEnum)ve);
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // TODO : 설정화면에서 키 변경할수 있도록 하기

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.O:
                        FolderOpenBt_Click(null, null);
                        break;
                    //case Key.OemComma:
                    //    SettingBt_Click(null, null);
                    //    break;
                    case Key.Q:
                        Close();
                        break;
                    case Key.E:
                        ExportThumbnailBt_Click(null, null);
                        break;
                    case Key.R:
                        RawCopyBt_Click(null, null);
                        break;
                    case Key.S:
                        OnlySelectedShowBt_Click(null, null);
                        break;
                    default:
                        break;
                }
            }
            else
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        if (IsFullScreen)
                        {
                            FullScreen();
                        }
                        break;
                    case Key.F5:
                        RefreshBt_Click(null, null);
                        break;
                    case Key.End:
                        LoadImage.LastImage();
                        break;
                    case Key.Home:
                        LoadImage.FirstImage();
                        break;
                    case Key.Left:
                        LoadImage.PrevImage();
                        break;
                    case Key.PageUp:
                        LoadImage.PrevImage(10);
                        break;
                    case Key.Up:
                        break;
                    case Key.Right:
                        LoadImage.NextIamge();
                        break;
                    case Key.PageDown:
                        LoadImage.NextIamge(10);
                        break;
                    case Key.Down:
                        break;
                    case Key.F:
                        FullScreen();
                        break;
                    case Key.B:
                        LoadImage.SelectEx();
                        break;
                    case Key.I:
                        LoadImage.ChangeExif();
                        break;
                    default:
                        break;
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                ExportThumbnailWindow.GetInstance().Close();
                RawCopyWindow.GetInstance().Close();

                Common.FileDelete("exiv2-ql-32.dll");
                Common.FileDelete("exiv2-ql-64.dll");
                Common.FileDelete("libraw.dll");
                Common.FileDelete("log4net.dat");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            App.Setting.Save();
            Application.Current.Shutdown();
        }

        public void FocusOut()
        {
            switch (Common.NowView)
            {
                case ViewEnum.Grid:
                    break;
                case ViewEnum.Single:
                    SingleViewCtrl.MainImg.Focus();
                    break;
                case ViewEnum.Full:
                    FullViewCtrl.MainImg.Focus();
                    break;
                default:
                    break;
            }
        }

        private void SelectImageFolderBt_MouseUp(object sender, MouseButtonEventArgs e)
        {
            OpenImageFolder();
        }

        public async void OpenImageFolder()
        {
            try
            {
                while (true)
                {
                    var dlg = new CommonOpenFileDialog();
                    dlg.Title = "RAW폴더열기";
                    dlg.IsFolderPicker = true;
                    //dlg.InitialDirectory = ExportPathTb.Text;
                    dlg.AddToMostRecentlyUsedList = false;
                    dlg.AllowNonFileSystemItems = false;
                    //dlg.DefaultDirectory = ExportPathTb.Text;
                    dlg.EnsureFileExists = true;
                    dlg.EnsurePathExists = true;
                    dlg.EnsureReadOnly = false;
                    dlg.EnsureValidNames = true;
                    dlg.Multiselect = false;
                    dlg.ShowPlacesList = true;

                    if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                    {
                        List<string> files = Directory.GetFiles(dlg.FileName, "*", SearchOption.TopDirectoryOnly).ToList();
                        files.Sort();

                        for (int i = 0; i < files.Count; i++)
                        {
                            string file = files[i];
                            if (Common.IsRawFile(file))
                            {
                                LoadImage.SetArg(file);
                                return;
                            }
                        }
                        _ = await Alert.Info("RAW파일이 없는 폴더입니다.\n다시 선택해주세요.");
                    }
                    else
                    {
                        return;
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private void SettingBt_Click(object sender, RoutedEventArgs e)
        {
            SettingWindow sw = new SettingWindow();
            if (sw.ShowDialog() == true)
            {

            }
        }

        private void FolderOpenBt_Click(object sender, RoutedEventArgs e)
        {
            OpenImageFolder();
        }

        private void RefreshBt_Click(object sender, RoutedEventArgs e)
        {
            if (LoadImage.NowDir == null)
            {
                return;
            }

            List<string> files = Directory.GetFiles(LoadImage.NowDir, "*", SearchOption.TopDirectoryOnly).ToList();
            if (files.Contains(LoadImage.NowImage.Path))
            {
                LoadImage.SetArg(LoadImage.NowImage.Path);
            }
            else
            {
                files.Sort();
                for (int i = 0; i < files.Count; i++)
                {
                    string file = files[i];
                    if (Common.IsRawFile(file))
                    {
                        LoadImage.SetArg(file);
                        return;
                    }
                }
                _ = Alert.Info("RAW파일이 없는 폴더입니다.\n다시 선택해주세요.");
            }
        }

        private void TopBd_MouseEnter(object sender, MouseEventArgs e)
        {
            MenuBar.Visibility = Visibility.Visible;
        }

        private void SelectImageFolderBt_MouseEnter(object sender, MouseEventArgs e)
        {
            if (Common.NowView == ViewEnum.Full)
            {
                MenuBar.Visibility = Visibility.Collapsed;
            }
        }

        private void FolderDivisionBt_Click(object sender, RoutedEventArgs e)
        {
            FolderDivisionWindow.GetInstance().ShowWindow(this);
        }

        private void ExifChangeBt_Click(object sender, RoutedEventArgs e)
        {
            ExifChangeWindow.GetInstance().ShowWindow(this);
        }

        private void AllDeselectBt_Click(object sender, RoutedEventArgs e)
        {
            LoadImage.AllDeselect();

        }

        private void OnlySelectedShowBt_Click(object sender, RoutedEventArgs e)
        {
            Common.IsOnlySelectedShow = !Common.IsOnlySelectedShow;
            if (Common.IsOnlySelectedShow)
            {
                OnlySelectedShowBt.Header = "모든 사진 보기";
                Title = OriginalTitle + " (선택한 사진만 보기)";
            }
            else
            {
                OnlySelectedShowBt.Header = "선택한 사진만 보기";
                Title = OriginalTitle;
            }
            
        }
    }
}
