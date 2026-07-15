using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using FastRawSelector.VIEW;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace FastRawSelector
{
    /// <summary>
    /// 메인 셸: 메뉴, 단축키, 뷰 전환(Grid/Single/Full), 전체화면, 폴더 열기.
    /// 이미지 상태·디코드는 LoadImage, 전역 헬퍼는 Common 에 위임한다.
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] Args = null;
        /// <summary>폴더 로드·UI 준비 완료. FrameworkElement.IsLoaded 와 별개.</summary>
        public bool IsWindowReady = false;

        bool IsFullScreen = false;
        /// <summary>전체화면 진입 직전 창 상태. 기본 Normal — 미진입 시 잘못 최대화되지 않게.</summary>
        private WindowState agoWindowState = WindowState.Normal;
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

            OriginalTitle = Title + Common.GetVersion(Application.ResourceAssembly, " v", 3);
            Title = OriginalTitle;

            if (App.Setting != null)
            {
                Topmost = App.Setting.AlwaysOnTop;
            }

            // Part 6: 창 위치/크기/상태 복원
            RestoreWindowBounds();

            LoadingGrid.Visibility = Visibility.Collapsed;
            if (Args != null && Args.Length != 0)
            {
                LoadImage.SetArg(Args[0]);
            }
            else
            {
                TryOpenLastFolderOnStartup();
            }

            // 시작 테마에 맞춰 아이콘/체크박스 크롬 확정
            RefreshThemeChrome();
            RefreshHotkeyTooltips();
            SingleViewCtrl.MainImg.Focus();
            IsWindowReady = true;

            // 기동 시 업데이트 확인 (설정 ON, 백그라운드 — 최신이면 무음)
            if (App.Setting != null && App.Setting.AutoCheckUpdate)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    UpdateChecker.CheckAsync(silentIfUpToDate: true, showErrors: false);
                }), DispatcherPriority.ApplicationIdle);
            }
        }

        /// <summary>설정 단축키·언어에 맞춰 버튼·체크박스 ToolTip 갱신.</summary>
        public void RefreshHotkeyTooltips()
        {
            try
            {
                string full = "F";
                if (App.Setting != null)
                {
                    full = App.Setting.KeyFullScreen ?? "F";
                }
                if (FullViewBt != null)
                {
                    FullViewBt.ToolTip = Loc.GetFormat("TipFull", full);
                }
                if (GridViewBt != null)
                {
                    GridViewBt.ToolTip = Loc.Get("TipGrid");
                }
                if (SingleViewBt != null)
                {
                    SingleViewBt.ToolTip = Loc.Get("TipSingle");
                }
                SingleViewCtrl.RefreshHotkeyTooltips();
                FullViewCtrl.RefreshHotkeyTooltips();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>언어 변경 후 메인·뷰 크롬 문자열 재적용.</summary>
        public void RefreshLanguage()
        {
            try
            {
                RefreshHotkeyTooltips();
                GridViewCtrl.ApplyLanguage();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>설정 OpenLastFolderOnStartup 시 최근 폴더의 첫 이미지를 연다.</summary>
        private void TryOpenLastFolderOnStartup()
        {
            try
            {
                if (App.Setting == null || !App.Setting.OpenLastFolderOnStartup)
                {
                    return;
                }
                string dir = App.Setting.LastFolderPath;
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    Log.Info("마지막 폴더 자동 열기 스킵: 경로 없음");
                    return;
                }

                var files = Directory.GetFiles(dir, "*", SearchOption.TopDirectoryOnly).ToList();
                files.Sort();
                foreach (var file in files)
                {
                    if (Common.IsRawFile(file)
                        || (App.Setting.AllowNotRawImage && Common.IsBitmapFile(file)))
                    {
                        Log.Info("마지막 폴더 자동 열기: " + file);
                        LoadImage.SetArg(file);
                        return;
                    }
                }
                Log.Info("마지막 폴더에 열 이미지 없음: " + dir);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
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
            if (Common.NowView == ViewEnum.Grid)
            {
                return;
            }
            // 전체화면이면 창 모드로 복귀 후 그리드
            if (IsFullScreen)
            {
                FullScreen(false, ViewEnum.Grid);
            }
            else
            {
                ViewChange(ViewEnum.Grid);
            }
        }

        private void SingleViewBt_Click(object sender, RoutedEventArgs e)
        {
            if (Common.NowView == ViewEnum.Single)
            {
                return;
            }
            // 전체화면에서만 창 상태 복원. 그리드→일반은 최대화하지 않음 (창 상태 유지).
            if (IsFullScreen)
            {
                FullScreen(false, ViewEnum.Single);
            }
            else
            {
                ViewChange(ViewEnum.Single);
            }
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

        /// <summary>테마 전환 후 아이콘·강조색 재적용.</summary>
        public void RefreshThemeChrome()
        {
            ApplyViewIconColors(Common.NowView);
            try
            {
                SingleViewCtrl.ApplyThemeChrome();
                FullViewCtrl.ApplyThemeChrome();
                GridViewCtrl.ApplyThemeChrome();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void ApplyViewIconColors(ViewEnum viewEnum)
        {
            // SetResourceReference 로 두면 테마 전환 시 App* 브러시가 자동 반영된다.
            SetViewIconBrush(GridViewIcon, GridViewBt, viewEnum == ViewEnum.Grid);
            SetViewIconBrush(SingleViewIcon, SingleViewBt, viewEnum == ViewEnum.Single);
            SetViewIconBrush(FullViewIcon, FullViewBt, viewEnum == ViewEnum.Full);
        }

        private static void SetViewIconBrush(
            MahApps.Metro.IconPacks.PackIconBoxIcons icon,
            System.Windows.Controls.Button button,
            bool active)
        {
            // 뷰 토글 전용 브러시 (라이트: 차분한 슬레이트 대비, 다크: 청록 강조)
            string key = active ? "AppViewActiveBrush" : "AppViewInactiveBrush";
            icon.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, key);
            if (button != null)
            {
                button.SetResourceReference(System.Windows.Controls.Control.ForegroundProperty, key);
            }
        }

        public void ViewChange(ViewEnum viewEnum)
        {
            Common.AgoView = Common.NowView;
            Common.NowView = viewEnum;
            ApplyViewIconColors(viewEnum);

            switch (viewEnum)
            {
                case ViewEnum.Grid:
                    GridViewCtrl.Visibility = Visibility.Visible;
                    SingleViewCtrl.Visibility = Visibility.Collapsed;
                    FullViewCtrl.Visibility = Visibility.Collapsed;
                    GridViewCtrl.RefreshItems();
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
            if (LoadImage.NowImage != null)
            {
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

        /// <summary>
        /// PreviewKeyDown: 메뉴에 포커스가 있어도 단축키가 동작하도록 터널링 단계에서 처리.
        /// 드롭다운이 열린 동안은 ←→ 등 메뉴 내비게이션에 맡긴다.
        /// </summary>
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 고정: Ctrl+O/,/Q/E/R/S  ←→ PgUp/Dn Home/End F5 Esc F1
            // 설정 가능: 선택 / EXIF / 전체화면 (ApplicationSetting.Key*)

            bool menuOpen = IsAnyTopLevelSubmenuOpen();

            if ((Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                switch (e.Key)
                {
                    case Key.O:
                        FolderOpenBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.OemComma:
                        SettingBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.Q:
                        Close();
                        e.Handled = true;
                        break;
                    case Key.E:
                        ExportThumbnailBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.R:
                        RawCopyBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.D:
                        FolderDivisionBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.T:
                        ExifChangeBt_Click(null, null);
                        e.Handled = true;
                        break;
                    case Key.S:
                        OnlySelectedShowBt_Click(null, null);
                        e.Handled = true;
                        break;
                    default:
                        break;
                }
                return;
            }

            // 메뉴 드롭다운 열린 중: 방향키/Esc 등은 메뉴에 양보
            if (menuOpen)
            {
                if (e.Key == Key.F1)
                {
                    HelpBt_Click(null, null);
                    e.Handled = true;
                }
                else if (e.Key == Key.F5)
                {
                    RefreshBt_Click(null, null);
                    e.Handled = true;
                }
                return;
            }

            Key selectKey = App.Setting != null ? App.Setting.GetSelectKey() : Key.B;
            Key exifKey = App.Setting != null ? App.Setting.GetExifKey() : Key.I;
            Key fullKey = App.Setting != null ? App.Setting.GetFullScreenKey() : Key.F;

            if (e.Key == Key.F1)
            {
                HelpBt_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                if (IsFullScreen)
                {
                    FullScreen();
                    e.Handled = true;
                }
            }
            else if (e.Key == Key.F5)
            {
                RefreshBt_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == Key.End)
            {
                LoadImage.LastImage();
                e.Handled = true;
            }
            else if (e.Key == Key.Home)
            {
                LoadImage.FirstImage();
                e.Handled = true;
            }
            else if (e.Key == Key.Left)
            {
                LoadImage.PrevImage();
                e.Handled = true;
            }
            else if (e.Key == Key.PageUp)
            {
                LoadImage.PrevImage(10);
                e.Handled = true;
            }
            else if (e.Key == Key.Right)
            {
                LoadImage.NextImage();
                e.Handled = true;
            }
            else if (e.Key == Key.PageDown)
            {
                LoadImage.NextImage(10);
                e.Handled = true;
            }
            else if (e.Key == Key.G && selectKey != Key.G && exifKey != Key.G && fullKey != Key.G)
            {
                GridViewBt_Click(null, null);
                e.Handled = true;
            }
            else if (e.Key == selectKey)
            {
                LoadImage.SelectEx();
                e.Handled = true;
            }
            else if (e.Key == exifKey)
            {
                LoadImage.ChangeExif();
                e.Handled = true;
            }
            else if (e.Key == fullKey)
            {
                // 그리드에서는 F 대신 싱글/풀 전환에 사용 — 전체화면 유지
                FullScreen();
                e.Handled = true;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                Log.Info("앱 종료 시작");
                if (Common.NowSelectorSetting != null)
                {
                    Common.NowSelectorSetting.Save();
                    Log.Info("선택 설정 저장 완료");
                }

                ExportThumbnailWindow.GetInstance().Close();
                RawCopyWindow.GetInstance().Close();
                // Part 6: 창 위치/크기 저장 (App.Setting.Save 직전)
                SaveWindowBounds();
                // AppData(Roaming) 데이터는 종료 시 삭제하지 않음
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
            App.Setting.Save();
            Log.Info("앱 종료");
            Application.Current.Shutdown();
        }

        /// <summary>설정에 저장된 메인 창 위치·크기·상태를 복원. 화면 밖이면 클램프 (Part 6).</summary>
        private void RestoreWindowBounds()
        {
            try
            {
                if (App.Setting == null)
                {
                    return;
                }
                double? w = App.Setting.WindowWidth;
                double? h = App.Setting.WindowHeight;
                double? left = App.Setting.WindowLeft;
                double? top = App.Setting.WindowTop;
                if (w.HasValue && h.HasValue && left.HasValue && top.HasValue
                    && w.Value >= 400 && h.Value >= 300
                    && !double.IsNaN(left.Value) && !double.IsNaN(top.Value)
                    && !double.IsInfinity(left.Value) && !double.IsInfinity(top.Value))
                {
                    Width = w.Value;
                    Height = h.Value;
                    Left = left.Value;
                    Top = top.Value;
                    ClampToVirtualScreen();
                }

                if (string.Equals(App.Setting.WindowState, "Maximized", StringComparison.OrdinalIgnoreCase))
                {
                    WindowState = WindowState.Maximized;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>종료 시 창 위치·크기·상태 저장. 최소화 중이면 RestoreBounds 사용 (Part 6).</summary>
        private void SaveWindowBounds()
        {
            try
            {
                if (App.Setting == null)
                {
                    return;
                }
                if (WindowState == WindowState.Minimized)
                {
                    // RestoreBounds 에 복원 예정 사각형
                    var rb = RestoreBounds;
                    App.Setting.WindowLeft = rb.Left;
                    App.Setting.WindowTop = rb.Top;
                    App.Setting.WindowWidth = rb.Width;
                    App.Setting.WindowHeight = rb.Height;
                    App.Setting.WindowState = "Normal";
                }
                else if (WindowState == WindowState.Maximized)
                {
                    var rb = RestoreBounds;
                    App.Setting.WindowLeft = rb.Left;
                    App.Setting.WindowTop = rb.Top;
                    App.Setting.WindowWidth = rb.Width;
                    App.Setting.WindowHeight = rb.Height;
                    App.Setting.WindowState = "Maximized";
                }
                else
                {
                    App.Setting.WindowLeft = Left;
                    App.Setting.WindowTop = Top;
                    App.Setting.WindowWidth = Width;
                    App.Setting.WindowHeight = Height;
                    App.Setting.WindowState = "Normal";
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>작업 영역 밖으로 완전히 나간 창을 가시 영역 안으로 보정.</summary>
        private void ClampToVirtualScreen()
        {
            double vw = SystemParameters.VirtualScreenWidth;
            double vh = SystemParameters.VirtualScreenHeight;
            double vx = SystemParameters.VirtualScreenLeft;
            double vy = SystemParameters.VirtualScreenTop;

            // 최소 80px 은 화면 안에 보이도록
            const double margin = 80;
            if (Left + Width < vx + margin)
            {
                Left = vx;
            }
            if (Top + Height < vy + margin)
            {
                Top = vy;
            }
            if (Left > vx + vw - margin)
            {
                Left = vx + vw - Math.Min(Width, vw);
            }
            if (Top > vy + vh - margin)
            {
                Top = vy + Math.Min(40, vh);
            }
            if (Width > vw)
            {
                Width = vw;
            }
            if (Height > vh)
            {
                Height = vh;
            }
        }

        public void FocusOut()
        {
            switch (Common.NowView)
            {
                case ViewEnum.Grid:
                    GridViewCtrl.Focus();
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

        /// <summary>
        /// MaterialDesign MenuItem 팝업은 HasDropShadow 시 SubMenuBorder Margin=5 로
        /// 헤더↔드롭다운 사이 투명 dead zone이 생긴다. 마우스가 그 구간을 지나면 서브메뉴가 닫힌다.
        /// 위쪽 여백을 없애고 팝업을 5px 올려 히트 영역을 이어 준다.
        /// </summary>
        private static void FixTopLevelMenuPopupGap(MenuItem menuItem)
        {
            if (menuItem == null)
            {
                return;
            }
            menuItem.ApplyTemplate();
            if (!(menuItem.Template?.FindName("PART_Popup", menuItem) is Popup popup))
            {
                return;
            }
            // 애니메이션/그림자 여백과 겹쳐 깜빡이지 않도록 팝업 애니메이션 끄고 간격만 보정
            popup.PopupAnimation = PopupAnimation.None;
            popup.VerticalOffset = -5;
            if (popup.Child is FrameworkElement child)
            {
                child.Margin = new Thickness(5, 0, 5, 5);
            }
        }

        private bool IsAnyTopLevelSubmenuOpen()
        {
            if (MainMenu == null)
            {
                return false;
            }
            return MainMenu.Items.OfType<MenuItem>().Any(mi => mi.IsSubmenuOpen);
        }

        private void TopLevelMenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            FixTopLevelMenuPopupGap(sender as MenuItem);
        }

        private void TopLevelMenuItem_SubmenuClosed(object sender, RoutedEventArgs e)
        {
            // 메뉴가 모두 닫히면 포커스를 이미지 쪽으로 되돌려 단축키가 메뉴에 묶이지 않게 함
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!IsAnyTopLevelSubmenuOpen() && IsActive)
                {
                    FocusOut();
                }
            }), DispatcherPriority.Background);
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
                    dlg.AddToMostRecentlyUsedList = false;
                    dlg.AllowNonFileSystemItems = false;
                    if (App.Setting != null && !string.IsNullOrEmpty(App.Setting.LastFolderPath)
                        && Directory.Exists(App.Setting.LastFolderPath))
                    {
                        dlg.InitialDirectory = App.Setting.LastFolderPath;
                        dlg.DefaultDirectory = App.Setting.LastFolderPath;
                    }
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
                            if (Common.IsRawFile(file)
                                || (App.Setting.AllowNotRawImage && Common.IsBitmapFile(file)))
                            {
                                LoadImage.SetArg(file);
                                return;
                            }
                        }
                        _ = await Alert.Info(App.Setting.AllowNotRawImage
                            ? Loc.Get("NoImageInFolder")
                            : Loc.Get("NoImageAllowRawOnly"));
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
            var sw = new SettingWindow
            {
                Owner = this
            };
            sw.ShowDialog();
            FocusOut();
        }

        private void HelpBt_Click(object sender, RoutedEventArgs e)
        {
            var hw = new HelpWindow
            {
                Owner = this
            };
            hw.ShowDialog();
            FocusOut();
        }

        private void LicenseBt_Click(object sender, RoutedEventArgs e)
        {
            var lw = new LicenseWindow
            {
                Owner = this
            };
            lw.ShowDialog();
            FocusOut();
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
                    if (Common.IsRawFile(file)
                        || (App.Setting.AllowNotRawImage && Common.IsBitmapFile(file)))
                    {
                        LoadImage.SetArg(file);
                        return;
                    }
                }
                _ = Alert.Info(App.Setting.AllowNotRawImage
                    ? Loc.Get("NoImageInFolder")
                    : Loc.Get("NoImageAllowRawOnly"));
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
            if (Common.NowView == ViewEnum.Grid)
            {
                GridViewCtrl.RefreshItems();
            }
        }
    }
}
