using FastRawSelector.MODEL;
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
    public static class Common
    {
        public static MainWindow Main { get; set; } // 메인화면
        //public static DateTime NowMonth { get; set; } = DateTime.Now; // 현재 표시 년월

        public static bool TestMode = false; // 테스트모드
        public static string RootPath = AppDomain.CurrentDomain.BaseDirectory; // 실행파일이 있는 루트폴더
        public static string TempFolderPath = Path.Combine(RootPath, "TEMP"); // 임시폴더
        public static string BackupFolderPath = Path.Combine(RootPath, "BACKUP"); // 백업폴더

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
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
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

    }
}
