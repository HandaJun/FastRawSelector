using FastRawSelector.MODEL;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 앱 전역 헬퍼·상태.
    /// UI 디스패치(Invoke), 경로(AppData/exe), RAW/비트맵 확장자 판별,
    /// 네이티브 DLL·log4net 초기화(InitNativeRuntime)를 담당한다.
    /// 코드비하인드 중심 구조에서 서비스 레이어 역할을 한다.
    /// </summary>
    public static class Common
    {
        public static MainWindow Main { get; set; } // 메인화면
        //public static DateTime NowMonth { get; set; } = DateTime.Now; // 현재 표시 년월

        public static bool TestMode = false; // 테스트모드
        public static string RootPath = AppDomain.CurrentDomain.BaseDirectory; // 실행파일이 있는 루트폴더
        public static string TempFolderPath = Path.Combine(RootPath, "TEMP"); // 임시폴더
        public static string BackupFolderPath = Path.Combine(RootPath, "BACKUP"); // 백업폴더
        public static string EnviromentTempFolderPath = Path.GetFullPath(Environment.GetEnvironmentVariable("temp") + "\\");

        /// <summary>
        /// %AppData%\Roaming\FastRawSelector — 앱 데이터 루트
        /// (설정, 로그, log4net 설정, 네이티브 DLL 일괄 관리)
        /// </summary>
        public static readonly string AppDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FastRawSelector");

        /// <summary>호환용 별칭 (AppDataPath 와 동일)</summary>
        public static readonly string RoamingAppDataPath = AppDataPath;

        /// <summary>%AppData%\Roaming\FastRawSelector\logs</summary>
        public static readonly string LogsPath = Path.Combine(AppDataPath, "logs");

        /// <summary>%AppData%\Roaming\FastRawSelector\native — libraw / exiv2</summary>
        public static readonly string NativePath = Path.Combine(AppDataPath, "native");

        public static ViewEnum AgoView = ViewEnum.Single;
        public static ViewEnum NowView = ViewEnum.Single;

        public static DateTime LastKeydown = DateTime.Now;
        public static SelectorSetting NowSelectorSetting { get; set; } = null;
        public static DateTime? NowSelectorSettingDate { get; set; } = null;

        public static SolidColorBrush HeadSaturdayBrush = new SolidColorBrush(Color.FromArgb(255, 40, 110, 180)); // 헤드 토요일 배경색
        public static SolidColorBrush HeadSundayBrush = new SolidColorBrush(Color.FromArgb(255, 203, 57, 57)); // 헤드 일요일 배경색
        public static SolidColorBrush HeadNormalBrush = new SolidColorBrush(Color.FromArgb(255, 221, 221, 221)); // 헤드 평일 배경색
        public static SolidColorBrush DaySaturdayBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 255)); // 토요일 배경색
        public static SolidColorBrush DaySundayBrush = new SolidColorBrush(Color.FromArgb(10, 255, 0, 0)); // 일요일 배경색
        public static SolidColorBrush DayNormalBrush = new SolidColorBrush(Color.FromArgb(10, 0, 0, 0)); // 평일 배경색
        public static SolidColorBrush TodayBorderBrush = new SolidColorBrush(Color.FromArgb(255, 63, 81, 181)); // 오늘 테두리색
        //public static SolidColorBrush TodayBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 187)); // 오늘 배경색
        public static SolidColorBrush TodayBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 250, 252, 177)); // 오늘 배경색

        public static SolidColorBrush MainBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 54, 69, 154)); // 메인색
        public static SolidColorBrush MainBrush = new SolidColorBrush(Colors.Cyan);
        public static SolidColorBrush GrayBrush = new SolidColorBrush(Colors.Gray);
        public static SolidColorBrush ErrorBackgroundBrush = new SolidColorBrush(Color.FromArgb(255, 203, 57, 57)); // 에러색

        public static SolidColorBrush AquaBrush = new SolidColorBrush(Color.FromArgb(255, 0, 151, 167)); // Aqua
        public static SolidColorBrush DarkGrayBrush = new SolidColorBrush(Color.FromArgb(255, 33, 33, 33));
        public static SolidColorBrush TransBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public static bool IsOnlySelectedShow = false;

        // 숫자만
        public static readonly Regex NotNumberRegex = new Regex("[^0-9]+");

        private static bool _loading = false;
        /// <summary>
        /// 로딩
        /// </summary>
        public static bool Loading
        {
            get
            {
                return _loading;
            }
            set
            {
                _loading = value;
                Invoke(() => Main.LoadingBd.Visibility = value ? Visibility.Visible : Visibility.Collapsed);
            }
        }

        /// <summary>
        /// MaterialDesign Dark/Light 테마 적용.
        /// PaletteHelper + Dark/Light 사전 + App* 토큰 브러시 + 메인 크롬 갱신.
        /// </summary>
        public static void ApplyTheme(string theme)
        {
            try
            {
                string mode = string.Equals(theme, "Light", StringComparison.OrdinalIgnoreCase) ? "Light" : "Dark";
                bool light = mode == "Light";

                try
                {
                    var paletteHelper = new MaterialDesignThemes.Wpf.PaletteHelper();
                    MaterialDesignThemes.Wpf.ITheme mdTheme = paletteHelper.GetTheme();
                    MaterialDesignThemes.Wpf.IBaseTheme baseTheme = light
                        ? (MaterialDesignThemes.Wpf.IBaseTheme)new MaterialDesignThemes.Wpf.MaterialDesignLightTheme()
                        : (MaterialDesignThemes.Wpf.IBaseTheme)new MaterialDesignThemes.Wpf.MaterialDesignDarkTheme();
                    mdTheme.SetBaseTheme(baseTheme);
                    paletteHelper.SetTheme(mdTheme);
                }
                catch (Exception ex)
                {
                    Log.ExceptionWithMsg("PaletteHelper 테마 적용 실패, 사전 교체 시도", ex);
                }

                string target = "MaterialDesignTheme." + mode + ".xaml";
                var merged = Application.Current.Resources.MergedDictionaries;

                ResourceDictionary oldTheme = null;
                foreach (var d in merged)
                {
                    if (d.Source != null)
                    {
                        string s = d.Source.OriginalString ?? "";
                        if (s.IndexOf("MaterialDesignTheme.Dark.xaml", StringComparison.OrdinalIgnoreCase) >= 0
                            || s.IndexOf("MaterialDesignTheme.Light.xaml", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            oldTheme = d;
                            break;
                        }
                    }
                }

                var newTheme = new ResourceDictionary
                {
                    Source = new Uri(
                        "pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/" + target,
                        UriKind.Absolute)
                };

                if (oldTheme != null)
                {
                    int idx = merged.IndexOf(oldTheme);
                    merged.RemoveAt(idx);
                    merged.Insert(idx, newTheme);
                }
                else
                {
                    merged.Insert(0, newTheme);
                }

                ApplyAppThemeTokens(light);

                // 코드에서 쓰는 정적 브러시 — 라이트: 차분한 파랑, 다크: 청록
                MainBrush = light
                    ? new SolidColorBrush(Color.FromRgb(0x15, 0x65, 0xC0))
                    : new SolidColorBrush(Color.FromRgb(0x00, 0x97, 0xA7));
                GrayBrush = light
                    ? new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75))
                    : new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E));
                AquaBrush = MainBrush;

                if (Main != null)
                {
                    var paper = Application.Current.TryFindResource("MaterialDesignPaper") as System.Windows.Media.Brush;
                    if (paper != null)
                    {
                        Main.Background = paper;
                        Main.MenuBar.Background = paper;
                    }
                    try
                    {
                        Main.RefreshThemeChrome();
                        Main.GridViewCtrl.ApplyTreeTheme();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }

                Log.Info("테마 적용: " + mode);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>App* DynamicResource 키 색을 Light/Dark 에 맞게 설정.</summary>
        private static void ApplyAppThemeTokens(bool light)
        {
            var r = Application.Current.Resources;
            if (light)
            {
                // 라이트 메인: 차분한 파랑 (Cyan 대신 Blue 800 계열)
                var accent = Color.FromRgb(0x15, 0x65, 0xC0);
                var accentDark = Color.FromRgb(0x0D, 0x47, 0xA1);
                var accentLight = Color.FromRgb(0x42, 0xA5, 0xF5);

                SetAppBrush(r, "AppChromeBorderBrush", Color.FromRgb(0xBD, 0xBD, 0xBD));
                SetAppBrush(r, "AppTitleIconBrush", Color.FromRgb(0x61, 0x61, 0x61));
                SetAppBrush(r, "AppPanelBorderBrush", Color.FromArgb(0x55, 0x00, 0x00, 0x00));
                SetAppBrush(r, "AppInputBackgroundBrush", Color.FromRgb(0xF5, 0xF5, 0xF5));
                SetAppBrush(r, "AppGridItemBgBrush", Color.FromArgb(0x0A, 0x00, 0x00, 0x00));
                SetAppBrush(r, "AppGridItemBorderBrush", Color.FromArgb(0x38, 0x00, 0x00, 0x00));
                SetAppBrush(r, "AppSplitterBrush", Color.FromArgb(0x40, 0x00, 0x00, 0x00));
                SetAppBrush(r, "AppDropZoneBrush", Color.FromArgb(0x28, accent.R, accent.G, accent.B));
                SetAppBrush(r, "AppAccentBrush", accent);
                SetAppBrush(r, "AppOnAccentBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppSecondaryTextBrush", Color.FromRgb(0x61, 0x61, 0x61));
                SetAppBrush(r, "AppIconBrush", Color.FromRgb(0x42, 0x42, 0x42));
                SetAppBrush(r, "AppMutedIconBrush", Color.FromRgb(0x9E, 0x9E, 0x9E));
                // 뷰 토글: 선택=메인 액센트(차분한 파랑), 비선택=연한 회색
                SetAppBrush(r, "AppViewActiveBrush", accent);
                SetAppBrush(r, "AppViewInactiveBrush", Color.FromRgb(0xB0, 0xBE, 0xC5));
                // EXIF 아이콘: 라이트에서 어두운 색
                SetAppBrush(r, "AppExifIconBrush", Color.FromRgb(0x37, 0x47, 0x4F));
                // 선택 배경 틴트 (사진 뒤)
                SetAppBrush(r, "AppSelectedOverlayBrush", Color.FromArgb(0x40, accent.R, accent.G, accent.B));
                SetAppBrush(r, "AppSelectedItemBgBrush", Color.FromArgb(0x28, accent.R, accent.G, accent.B));
                // MaterialDesign Raised 버튼 Primary 도 파랑으로
                SetAppBrush(r, "PrimaryHueLightBrush", accentLight);
                SetAppBrush(r, "PrimaryHueMidBrush", accent);
                SetAppBrush(r, "PrimaryHueDarkBrush", accentDark);
                SetAppBrush(r, "PrimaryHueLightForegroundBrush", Color.FromRgb(0x21, 0x21, 0x21));
                SetAppBrush(r, "PrimaryHueMidForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "PrimaryHueDarkForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            }
            else
            {
                var accent = Color.FromRgb(0x00, 0x97, 0xA7);
                var accentDark = Color.FromRgb(0x00, 0x6E, 0x7A);
                var accentLight = Color.FromRgb(0x4D, 0xD0, 0xE1);

                SetAppBrush(r, "AppChromeBorderBrush", Color.FromRgb(0xE0, 0xE0, 0xE0));
                SetAppBrush(r, "AppTitleIconBrush", Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppPanelBorderBrush", Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppInputBackgroundBrush", Color.FromArgb(0x18, 0x3F, 0x51, 0xB5));
                SetAppBrush(r, "AppGridItemBgBrush", Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppGridItemBorderBrush", Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppSplitterBrush", Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppDropZoneBrush", Color.FromArgb(0x33, accent.R, accent.G, accent.B));
                SetAppBrush(r, "AppAccentBrush", accent);
                SetAppBrush(r, "AppOnAccentBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppSecondaryTextBrush", Color.FromRgb(0xB0, 0xB0, 0xB0));
                SetAppBrush(r, "AppIconBrush", Color.FromRgb(0xEE, 0xEE, 0xEE));
                SetAppBrush(r, "AppMutedIconBrush", Color.FromRgb(0x9E, 0x9E, 0x9E));
                // 뷰 토글: 선택=메인 액센트(청록), 비선택=회색
                SetAppBrush(r, "AppViewActiveBrush", accent);
                SetAppBrush(r, "AppViewInactiveBrush", Color.FromRgb(0x75, 0x75, 0x75));
                // EXIF 아이콘: 다크에서는 밝은 색
                SetAppBrush(r, "AppExifIconBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "AppSelectedOverlayBrush", Color.FromArgb(0x50, 0x00, 0xBC, 0xD4));
                SetAppBrush(r, "AppSelectedItemBgBrush", Color.FromArgb(0x40, 0x00, 0xBC, 0xD4));
                SetAppBrush(r, "PrimaryHueLightBrush", accentLight);
                SetAppBrush(r, "PrimaryHueMidBrush", accent);
                SetAppBrush(r, "PrimaryHueDarkBrush", accentDark);
                SetAppBrush(r, "PrimaryHueLightForegroundBrush", Color.FromRgb(0x21, 0x21, 0x21));
                SetAppBrush(r, "PrimaryHueMidForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
                SetAppBrush(r, "PrimaryHueDarkForegroundBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            }
            // 사진 오버레이 / 하단 라벨 — 테마 무관 흰색
            SetAppBrush(r, "AppPhotoOverlayBgBrush", Color.FromArgb(0x99, 0x00, 0x00, 0x00));
            SetAppBrush(r, "AppPhotoOverlayFgBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
            SetAppBrush(r, "AppBottomLabelBrush", Color.FromRgb(0xFF, 0xFF, 0xFF));
        }

        private static void SetAppBrush(ResourceDictionary r, string key, Color color)
        {
            r[key] = new SolidColorBrush(color);
        }

        /// <summary>
        /// UI 스레드로 액션을 마샬링한다. Action 확장 메서드 형태:
        /// <c>Common.Invoke(() =&gt; { ... })</c> 또는 <c>(() =&gt; { ... }).Invoke()</c>.
        /// 이미 UI 스레드면 동기 실행, 아니면 Dispatcher.Invoke.
        /// </summary>
        public static void Invoke(this Action act, FrameworkElement fe = null, DispatcherPriority priority = DispatcherPriority.Input)
        {
            try
            {
                Dispatcher dis;
                if (fe == null)
                {
                    dis = App.Current.Dispatcher;
                }
                else
                {
                    dis = fe.Dispatcher;
                }

                if (dis == null || dis.CheckAccess())
                {
                    act();
                }
                else
                {
                    dis.Invoke(priority, act);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void SetTimeout(this Action act, int milliseconds = 0, bool invokeFlg = false)
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(milliseconds);
                if (invokeFlg)
                {
                    Invoke(() =>
                    {
                        act();
                    });
                }
                else
                {
                    act();
                }
            });
        }

        /// <summary>
        /// 버전
        /// </summary>
        /// <param name="asb"></param>
        /// <param name="prefix"></param>
        /// <param name="step"></param>
        /// <returns></returns>
        public static string GetVersion(Assembly asb, string prefix = "v", int step = 4)
        {
            var thisVer = asb.GetName().Version;
            switch (step)
            {
                case 1:
                    return $"{prefix}{thisVer.Major}";
                case 2:
                    return $"{prefix}{thisVer.Major}.{thisVer.Minor}";
                case 3:
                    return $"{prefix}{thisVer.Major}.{thisVer.Minor}.{thisVer.Build}";
                default:
                    return $"{prefix}{thisVer}";
            }
        }

        public static string GetSaveFilePath(string filter = "All File (*.*)|*.*", string defaultExt = "", string fileName = "", string initialDirectory = "")
        {
            var dialog = new SaveFileDialog
            {
                Filter = filter
            };
            if (!string.IsNullOrEmpty(defaultExt))
            {
                dialog.DefaultExt = defaultExt;
            }
            if (!string.IsNullOrEmpty(fileName))
            {
                dialog.FileName = fileName;
            }

            if (!string.IsNullOrEmpty(initialDirectory))
            {
                dialog.InitialDirectory = initialDirectory;
            }

            if (dialog.ShowDialog() == true)
            {
                string file = dialog.FileNames[0] ?? "";
                if (!string.IsNullOrEmpty(file))
                {
                    return file;
                }
            }
            return null;
        }

        public static void Try(this Action act)
        {
            try
            {
                act();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        public static void CreateDirectory(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        public static void CreateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            if (!File.Exists(path))
            {
                File.WriteAllText(path, "");
            }
        }

        public static bool IsFileLocked(string filePath)
        {
            try
            {
                using (File.Open(filePath, FileMode.Open)) { }
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                return errorCode == 32 || errorCode == 33;
            }

            return false;
        }

        public static int Map(int x, int in_min, int in_max, int out_min, int out_max)
        {
            return (x - in_min) * (out_max - out_min) / (in_max - in_min) + out_min;
        }

        public static void FileDelete(string path)
        {
            try
            {
                if (!path.Contains("\\"))
                {
                    path = Path.Combine(RootPath, path);
                }
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                try
                {
                    string fileName = Path.GetFileNameWithoutExtension(path);
                    string batFileName = Path.Combine(EnviromentTempFolderPath, fileName + "Delete.bat");
                    FileDelete(batFileName);

                    string batchCommands = string.Empty;
                    batchCommands += "@ECHO OFF\n";
                    batchCommands += "ping 127.0.0.1 > nul\n";
                    batchCommands += "echo j | del /F ";
                    batchCommands += "\"" + path + "\"\n";
                    batchCommands += "echo j | del \"" + batFileName + "\"";
                    File.WriteAllText(batFileName, batchCommands);

                    Process p = new Process();
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.CreateNoWindow = true;
                    p.StartInfo.FileName = batFileName;
                    p.Start();
                }
                catch (Exception)
                {
                }
              
            }
        }

        public static void DirectoryDelete(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    Directory.Delete(path, true);
                }
            }
            catch (Exception)
            {
            }
        }

        public static T CloneXamlElement<T>(T source) where T : class
        {
            if (source == null)
                return null;

            object cloned = null;
            using (var stream = new MemoryStream())
            {
                XamlWriter.Save(source, stream);
                stream.Seek(0, SeekOrigin.Begin);
                cloned = XamlReader.Load(stream);
            }

            return (cloned is T) ? (T)cloned : null;

        }

        public static bool CheckBright(Color c)
        {
            return c.R * 0.2126 + c.G * 0.7152 + c.B * 0.0722 <= 255 / 2 + 1;
        }

        public static string GetMaxText(string text)
        {
            if (text == null)
            {
                return "";
            }
            else
            {
                if (text.Length > 32766)
                {
                    return text.Substring(0, 32765);
                }
                else
                {
                    return text;
                }
            }
        }

        public static Border CreateSelectorBorder(string name)
        {
            TextBlock tb = new TextBlock()
            {
                Text = name,
                Foreground = AquaBrush,
                FontSize = 15
            };
            Border bd = new Border()
            {
                BorderBrush = AquaBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(7, 0, 7, 1),
                Background = DarkGrayBrush,
                Margin = new Thickness(5),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            bd.Child = tb;
            return bd;
        }
        /// <summary>확장자로 카메라 RAW 파일 여부를 판별한다.</summary>
        public static bool IsRawFile(string path)
        {
            try
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".3fr":
                    case ".ari":
                    case ".arw":
                    case ".bay":
                    case ".braw":
                    case ".crw":
                    case ".cr2":
                    case ".cr3":
                    case ".cap":
                    case ".dcs":
                    case ".dcr":
                    case ".dng":
                    case ".drf":
                    case ".eip":
                    case ".erf":
                    case ".fff":
                    case ".gpr":
                    case ".iiq":
                    case ".k25":
                    case ".kdc":
                    case ".mdc":
                    case ".mef":
                    case ".mos":
                    case ".mrw":
                    case ".nef":
                    case ".nrw":
                    case ".obm":
                    case ".orf":
                    case ".pef":
                    case ".ptx":
                    case ".pxn":
                    case ".r3d":
                    case ".raf":
                    case ".raw":
                    case ".rwl":
                    case ".rw2":
                    case ".rwz":
                    case ".sr2":
                    case ".srf":
                    case ".srw":
                    case ".x3f":
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>확장자로 일반 비트맵(JPEG/PNG 등) 여부를 판별한다. AllowNotRawImage 와 함께 사용.</summary>
        public static bool IsBitmapFile(string path)
        {
            try
            {
                switch (Path.GetExtension(path).ToLower())
                {
                    case ".jpeg":
                    case ".jpg":
                    case ".gif":
                    case ".tiff":
                    case ".png":
                    case ".webp":
                    case ".ico":
                    case ".bmp":
                        return true;
                    default:
                        return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetFolder(string title, string initialDirectory, Action<string> afterAct)
        {
            try
            {
                var dlg = new CommonOpenFileDialog();
                dlg.Title = title;
                dlg.IsFolderPicker = true;
                if (!string.IsNullOrEmpty(initialDirectory))
                {
                    dlg.InitialDirectory = initialDirectory;
                    dlg.DefaultDirectory = initialDirectory;
                }
                dlg.AddToMostRecentlyUsedList = false;
                dlg.AllowNonFileSystemItems = false;
                dlg.EnsureFileExists = true;
                dlg.EnsurePathExists = true;
                dlg.EnsureReadOnly = false;
                dlg.EnsureValidNames = true;
                dlg.Multiselect = false;
                dlg.ShowPlacesList = true;

                if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    afterAct?.Invoke(dlg.FileName);
                    return dlg.FileName;
                }
            }
            catch (Exception)
            {
            }

            return null;
        }

        public static void MoveCenter(Window owner, Window child)
        {
            if (owner.WindowState == WindowState.Normal)
            {
                child.Left = owner.Left + owner.Width / 2 - child.Width / 2;
                child.Top = owner.Top + owner.Height / 2 - child.Height / 2;
            }
            else
            {
                WindowInteropHelper windowInteropHelper = new WindowInteropHelper(owner);
                var screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
                var screenBounds = screen.Bounds;
                child.Left = screenBounds.Left + screenBounds.Width / 2 - child.Width / 2;
                child.Top = screenBounds.Top + screenBounds.Height / 2 - child.Height / 2;
            }
        }

        static bool is64BitProcess = (IntPtr.Size == 8);
        public static bool is64BitOperatingSystem = is64BitProcess || InternalCheckIsWow64();

        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process(
            [In] IntPtr hProcess,
            [Out] out bool wow64Process
        );

        public static bool InternalCheckIsWow64()
        {
            if ((Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor >= 1) ||
                Environment.OSVersion.Version.Major >= 6)
            {
                using (Process p = Process.GetCurrentProcess())
                {
                    bool retVal;
                    if (!IsWow64Process(p.Handle, out retVal))
                    {
                        return false;
                    }
                    return retVal;
                }
            }
            else
            {
                return false;
            }
        }

        public static Uri GetUri(string fileName)
        {
            return new Uri("pack://application:,,,/" + fileName);
        }

        public static Stream UriToStream(Uri uri)
        {
            return Application.GetResourceStream(uri).Stream;
        }
        public static Stream GetStreamFromResource(string fileName)
        {
            return UriToStream(GetUri(fileName));
        }

        public static void StreamToFile(Stream stream, string output)
        {
            using (FileStream fileStream = System.IO.File.Create(output, (int)stream.Length))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, (int)bytesInStream.Length);
                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            }
        }

        public static void FileNameToFile(string fileName, string output)
        {
            Stream stream = GetStreamFromResource(fileName);
            using (FileStream fileStream = System.IO.File.Create(output, (int)stream.Length))
            {
                byte[] bytesInStream = new byte[stream.Length];
                stream.Read(bytesInStream, 0, (int)bytesInStream.Length);
                fileStream.Write(bytesInStream, 0, bytesInStream.Length);
            }
            stream?.Close();
        }

        public static void FileNameToRoot(string fileName)
        {
            FileNameToFile(fileName, Path.Combine(RootPath, fileName));
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool SetDllDirectory(string lpPathName);

        /// <summary>
        /// 리소스를 대상 경로에 추출. 동일 크기 파일이 있으면 건너뜀(매 기동 I/O 감소).
        /// </summary>
        public static void EnsureResourceFile(string fileName, string destDir)
        {
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            string output = Path.Combine(destDir, fileName);
            Stream stream = null;
            try
            {
                stream = GetStreamFromResource(fileName);
                long len = stream.Length;
                if (File.Exists(output) && new FileInfo(output).Length == len)
                {
                    return;
                }

                using (FileStream fileStream = File.Create(output, (int)len))
                {
                    byte[] bytesInStream = new byte[len];
                    int offset = 0;
                    while (offset < len)
                    {
                        int n = stream.Read(bytesInStream, offset, (int)len - offset);
                        if (n <= 0)
                        {
                            break;
                        }
                        offset += n;
                    }
                    fileStream.Write(bytesInStream, 0, offset);
                }
            }
            finally
            {
                stream?.Close();
            }
        }

        /// <summary>
        /// %AppData%\Roaming\FastRawSelector 아래에 설정·로그·네이티브 DLL을 준비한다.
        /// 종료 시 삭제하지 않음.
        /// </summary>
        public static void InitNativeRuntime()
        {
            Directory.CreateDirectory(AppDataPath);
            Directory.CreateDirectory(LogsPath);
            Directory.CreateDirectory(NativePath);
            try
            {
                Directory.CreateDirectory(GridThumbCache.ThumbsDir);
            }
            catch
            {
            }

            // 네이티브 DLL
            EnsureResourceFile("libraw.dll", NativePath);
            if (Environment.Is64BitProcess)
            {
                EnsureResourceFile("exiv2-ql-64.dll", NativePath);
            }
            else
            {
                EnsureResourceFile("exiv2-ql-32.dll", NativePath);
            }

            // log4net 설정 + 로그 절대 경로 패치
            // (리소스 갱신 시 크기 변경으로 재추출. 기존 AppData 파일은 경로 패치만)
            EnsureResourceFile("log4net.dat", AppDataPath);
            string logConfigPath = Path.Combine(AppDataPath, "log4net.dat");
            PatchLog4NetFilePaths(logConfigPath);

            // DllImport("libraw") / exiv2-ql-*.dll 가 NativePath를 찾도록 등록
            SetDllDirectory(NativePath);

            // 예전 위치 잔여 파일 정리(실패해도 무시)
            CleanupLegacyRuntimeFiles();

            var logConfig = new FileInfo(logConfigPath);
            log4net.Config.XmlConfigurator.ConfigureAndWatch(logConfig);

            Log.Info($"네이티브 런타임 초기화 완료. AppData={AppDataPath}, Native={NativePath}, Logs={LogsPath}, Process64={Environment.Is64BitProcess}");
        }

        /// <summary>exe 폴더 / LocalAppData 등 이전 배치 잔여 정리</summary>
        private static void CleanupLegacyRuntimeFiles()
        {
            TryDeleteQuiet(Path.Combine(RootPath, "libraw.dll"));
            TryDeleteQuiet(Path.Combine(RootPath, "exiv2-ql-32.dll"));
            TryDeleteQuiet(Path.Combine(RootPath, "exiv2-ql-64.dll"));
            TryDeleteQuiet(Path.Combine(RootPath, "log4net.dat"));

            // 묶음7: LocalAppData\FastRawSelector
            string legacyLocal = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FastRawSelector");
            TryDeleteQuiet(Path.Combine(legacyLocal, "log4net.dat"));
            TryDeleteQuiet(Path.Combine(legacyLocal, "native", "libraw.dll"));
            TryDeleteQuiet(Path.Combine(legacyLocal, "native", "exiv2-ql-32.dll"));
            TryDeleteQuiet(Path.Combine(legacyLocal, "native", "exiv2-ql-64.dll"));
            TryDeleteDirectoryQuiet(Path.Combine(legacyLocal, "native"));
            TryDeleteDirectoryQuiet(legacyLocal);

            // FastRawSelectorSetting.yaml 은 ApplicationSetting.Load 에서 Roaming 으로 마이그레이션
        }

        private static void TryDeleteDirectoryQuiet(string path)
        {
            try
            {
                if (Directory.Exists(path) && Directory.GetFileSystemEntries(path).Length == 0)
                {
                    Directory.Delete(path);
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// log4net.dat 의 상대 경로(.\logs\)를 AppDataPath\logs\ 절대 경로로 바꾼다.
        /// </summary>
        private static void PatchLog4NetFilePaths(string logConfigPath)
        {
            try
            {
                if (!File.Exists(logConfigPath))
                {
                    return;
                }

                string xml = File.ReadAllText(logConfigPath);
                // RollingFileAppender File 은 접두사 + DatePattern 파일명
                string logsPrefix = LogsPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                    + Path.DirectorySeparatorChar;

                string patched = xml
                    .Replace(@".\logs\", logsPrefix)
                    .Replace(@"./logs/", logsPrefix);

                if (patched != xml)
                {
                    File.WriteAllText(logConfigPath, patched);
                }
                else if (!xml.Contains(logsPrefix))
                {
                    // 이미 다른 절대 경로로 패치된 경우 현재 LogsPath 로 재기록
                    // value="...\logs\DEBUG" 형태를 현재 LogsPath 기준으로 맞춤
                    patched = System.Text.RegularExpressions.Regex.Replace(
                        xml,
                        @"value\s*=\s*""[^""]*[\\/]logs[\\/](DEBUG|INFO|ERROR)""",
                        m => "value=\"" + logsPrefix + m.Groups[1].Value + "\"",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (patched != xml)
                    {
                        File.WriteAllText(logConfigPath, patched);
                    }
                }
            }
            catch
            {
            }
        }

        private static void TryDeleteQuiet(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

    }
}
