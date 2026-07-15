using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// 폴더 분류: 확장자/날짜/카메라/렌즈 기준 하위 폴더로 복사 또는 이동.
    /// 기본은 복사(원본 유지). hide-on-close 싱글톤.
    /// </summary>
    public partial class FolderDivisionWindow : Window
    {
        private bool IsWindowReady = false;

        private readonly static FolderDivisionWindow _instance = new FolderDivisionWindow();
        public static FolderDivisionWindow GetInstance()
        {
            return _instance;
        }

        private FolderDivisionWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void TitleGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                DragMove();
            }
        }

        private void CloseBt_Click(object sender, RoutedEventArgs e)
        {
            Hide();
        }

        public void Init()
        {
            SourcePathTb.Text = LoadImage.NowDir ?? "";
            SourcePathTb.IsEnabled = false;
            SourcePathOpenBt.IsEnabled = false;

            if (LoadImage.NowDir == null)
            {
                AllCbi.IsEnabled = false;
                SelectedCbi.IsEnabled = false;
                SourceTargetCb.SelectedIndex = 2;
                SourcePathTb.IsEnabled = true;
                SourcePathOpenBt.IsEnabled = true;
            }
            else
            {
                AllCbi.IsEnabled = true;
                SourceTargetCb.SelectedIndex = 0;

                int selectedCount = 0;
                if (Common.NowSelectorSetting != null)
                {
                    lock (Common.NowSelectorSetting.SyncRoot)
                    {
                        selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
                    }
                }
                SelectedCbi.Content = Loc.Get("DivisionSelected") + " (" + selectedCount + ")";
                SelectedCbi.IsEnabled = selectedCount > 0;

                // 기본 출력: 상위 폴더\현재폴더명_classified
                try
                {
                    string parent = Directory.GetParent(LoadImage.NowDir)?.FullName;
                    string name = Path.GetFileName(LoadImage.NowDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name))
                    {
                        OutputPathTb.Text = Path.Combine(parent, name + "_classified");
                    }
                    else
                    {
                        OutputPathTb.Text = LoadImage.NowDir + "_classified";
                    }
                }
                catch
                {
                    OutputPathTb.Text = "";
                }
            }

            CopyRb.IsChecked = true;
            CriteriaCb.SelectedIndex = 0;
            DivisionPb.Value = 0;
            DivisionPb.Visibility = Visibility.Collapsed;
        }

        public void ShowWindow(Window owner)
        {
            Common.MoveCenter(owner, this);
            IsWindowReady = false;
            Show();
            Init();
            BringToFront();
            IsWindowReady = true;
        }

        private void SourceTargetCb_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsWindowReady || e.AddedItems == null || e.AddedItems.Count == 0)
            {
                return;
            }
            if (e.AddedItems[0] is ComboBoxItem cbi)
            {
                bool specified = cbi.Tag != null && cbi.Tag.ToString() == "SpecifiedFolder";
                SourcePathTb.IsEnabled = specified;
                SourcePathOpenBt.IsEnabled = specified;
                if (!specified && LoadImage.NowDir != null)
                {
                    SourcePathTb.Text = LoadImage.NowDir;
                }
            }
        }

        private void SourcePathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerSource"), SourcePathTb.Text, f =>
            {
                SourcePathTb.Text = f;
            });
            BringToFront();
        }

        private void OutputPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerDivisionOut"), OutputPathTb.Text, f =>
            {
                OutputPathTb.Text = f;
            });
            BringToFront();
        }

        private void WindowAlert(string msg, bool closeFlg = false)
        {
            Common.Invoke(() =>
            {
                DivisionPb.Visibility = Visibility.Collapsed;
                DivisionBt.IsEnabled = true;
                _ = Alert.Info(msg, afterAct: () =>
                {
                    if (closeFlg)
                    {
                        Hide();
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

        private void DivisionBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OutputPathTb.Text))
            {
                WindowAlert(Loc.Get("DivisionNeedOutput"));
                return;
            }

            string criteria = "Extension";
            if (CriteriaCb.SelectedItem is ComboBoxItem crit && crit.Tag != null)
            {
                criteria = crit.Tag.ToString();
            }

            string sourceMode = "All";
            if (SourceTargetCb.SelectedItem is ComboBoxItem src && src.Tag != null)
            {
                sourceMode = src.Tag.ToString();
            }

            bool move = MoveRb.IsChecked == true;
            string outputRoot = OutputPathTb.Text.Trim();
            string sourcePath = SourcePathTb.Text;

            List<string> files;
            try
            {
                files = CollectSourceFiles(sourceMode, sourcePath);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                WindowAlert(Loc.GetFormat("DivisionCollectFail", ex.Message));
                return;
            }

            if (files == null || files.Count == 0)
            {
                WindowAlert(Loc.Get("DivisionNoFiles"));
                return;
            }

            if (move)
            {
                var confirm = MessageBox.Show(
                    Loc.GetFormat("DivisionMoveConfirm", files.Count),
                    Loc.Get("DivisionTitle"),
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);
                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            DivisionBt.IsEnabled = false;
            DivisionPb.Visibility = Visibility.Visible;
            DivisionPb.Value = 0;

            Log.Info($"폴더 분류 시작: mode={sourceMode}, criteria={criteria}, move={move}, count={files.Count}, out={outputRoot}");

            Task.Run(() =>
            {
                RunDivision(files, criteria, outputRoot, move);
            });
        }

        private List<string> CollectSourceFiles(string sourceMode, string sourcePath)
        {
            switch (sourceMode)
            {
                case "Selected":
                    {
                        if (Common.NowSelectorSetting == null || LoadImage.NowDir == null)
                        {
                            return new List<string>();
                        }
                        List<string> names;
                        lock (Common.NowSelectorSetting.SyncRoot)
                        {
                            names = Common.NowSelectorSetting.SelectedSet.ToList();
                        }
                        var list = new List<string>();
                        foreach (var name in names)
                        {
                            string full = Path.Combine(LoadImage.NowDir, name);
                            if (File.Exists(full))
                            {
                                list.Add(full);
                            }
                        }
                        return list;
                    }
                case "SpecifiedFolder":
                    {
                        if (string.IsNullOrEmpty(sourcePath) || !Directory.Exists(sourcePath))
                        {
                            throw new DirectoryNotFoundException(Loc.Get("DivisionSourceMissing"));
                        }
                        return Directory.GetFiles(sourcePath, "*", SearchOption.TopDirectoryOnly)
                            .Where(f => Common.IsRawFile(f) || (App.Setting != null && App.Setting.AllowNotRawImage && Common.IsBitmapFile(f)))
                            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                            .ToList();
                    }
                case "All":
                default:
                    {
                        if (LoadImage.ImageList != null && LoadImage.ImageList.Count > 0)
                        {
                            return LoadImage.ImageList.Keys.ToList();
                        }
                        if (LoadImage.NowDir != null && Directory.Exists(LoadImage.NowDir))
                        {
                            return Directory.GetFiles(LoadImage.NowDir, "*", SearchOption.TopDirectoryOnly)
                                .Where(f => Common.IsRawFile(f) || (App.Setting != null && App.Setting.AllowNotRawImage && Common.IsBitmapFile(f)))
                                .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
                                .ToList();
                        }
                        return new List<string>();
                    }
            }
        }

        private void RunDivision(List<string> files, string criteria, string outputRoot, bool move)
        {
            int ok = 0;
            int fail = 0;
            int skip = 0;
            int total = files.Count;
            int done = 0;

            try
            {
                Directory.CreateDirectory(outputRoot);

                foreach (var file in files)
                {
                    try
                    {
                        if (!File.Exists(file))
                        {
                            Interlocked.Increment(ref skip);
                            continue;
                        }

                        string group = ResolveGroupName(file, criteria);
                        group = SanitizeFolderName(group);
                        string destDir = Path.Combine(outputRoot, group);
                        Directory.CreateDirectory(destDir);

                        string destFile = Path.Combine(destDir, Path.GetFileName(file));
                        destFile = GetUniquePath(destFile);

                        if (move)
                        {
                            File.Move(file, destFile);
                        }
                        else
                        {
                            File.Copy(file, destFile, false);
                        }
                        Interlocked.Increment(ref ok);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref fail);
                        Log.ExceptionWithMsg(file, ex);
                    }

                    int d = Interlocked.Increment(ref done);
                    if (d % 5 == 0 || d == total)
                    {
                        double pct = total > 0 ? Math.Min(100d, (double)d / total * 100d) : 100d;
                        Common.Invoke(() => { DivisionPb.Value = pct; });
                    }
                }

                Log.Info($"폴더 분류 완료: ok={ok}, fail={fail}, skip={skip}, total={total}, out={outputRoot}");
                WindowAlert(Loc.GetFormat("DivisionDone", ok, fail, skip, outputRoot), true);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                WindowAlert(Loc.GetFormat("DivisionError", ex.Message));
            }
        }

        /// <summary>분류 폴더명 결정.</summary>
        private static string ResolveGroupName(string filePath, string criteria)
        {
            switch (criteria)
            {
                case "Date":
                    try
                    {
                        return File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd");
                    }
                    catch
                    {
                        return "Unknown_Date";
                    }
                case "Camera":
                    return GetExifLabelValue(filePath, new[] { "Camera model", "Model", "Camera Model Name" })
                        ?? "Unknown_Camera";
                case "Lens":
                    return GetExifLabelValue(filePath, new[] { "Lens", "Lens model", "Lens Model", "LensID" })
                        ?? "Unknown_Lens";
                case "Extension":
                default:
                    {
                        string ext = Path.GetExtension(filePath);
                        if (string.IsNullOrEmpty(ext))
                        {
                            return "Unknown_Ext";
                        }
                        return ext.TrimStart('.').ToUpperInvariant();
                    }
            }
        }

        /// <summary>
        /// ImageList 캐시 EXIF 또는 MetaProvider 로 라벨 매칭 값을 찾는다.
        /// </summary>
        private static string GetExifLabelValue(string filePath, string[] labels)
        {
            try
            {
                SortedDictionary<string, (string, string)> exif = null;
                if (LoadImage.ImageList != null && LoadImage.ImageList.ContainsKey(filePath))
                {
                    exif = LoadImage.ImageList[filePath].Exif;
                }
                if (exif == null || exif.Count == 0)
                {
                    if (Common.IsRawFile(filePath))
                    {
                        var meta = new MetaProvider(filePath);
                        exif = meta.GetExif();
                        if (LoadImage.ImageList != null && LoadImage.ImageList.ContainsKey(filePath))
                        {
                            LoadImage.ImageList[filePath].Exif = exif;
                        }
                    }
                }
                if (exif == null)
                {
                    return null;
                }

                foreach (var kv in exif)
                {
                    string label = kv.Value.Item1 ?? "";
                    string value = kv.Value.Item2 ?? "";
                    foreach (var want in labels)
                    {
                        if (label.Equals(want, StringComparison.OrdinalIgnoreCase)
                            || label.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value.Trim();
                            }
                        }
                    }
                }

                // 키 이름에도 시도
                foreach (var kv in exif)
                {
                    foreach (var want in labels)
                    {
                        if (kv.Key != null && kv.Key.IndexOf(want.Replace(" ", ""), StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            if (!string.IsNullOrWhiteSpace(kv.Value.Item2))
                            {
                                return kv.Value.Item2.Trim();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(filePath, ex);
            }
            return null;
        }

        private static string SanitizeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "Unknown";
            }
            var sb = new StringBuilder(name.Trim());
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                sb.Replace(c, '_');
            }
            // 경로 구분자 등
            sb.Replace('/', '_');
            sb.Replace('\\', '_');
            string s = sb.ToString().Trim();
            if (s.Length > 80)
            {
                s = s.Substring(0, 80).Trim();
            }
            return string.IsNullOrEmpty(s) ? "Unknown" : s;
        }

        private static string GetUniquePath(string path)
        {
            if (!File.Exists(path))
            {
                return path;
            }
            string dir = Path.GetDirectoryName(path);
            string name = Path.GetFileNameWithoutExtension(path);
            string ext = Path.GetExtension(path);
            int i = 1;
            string candidate;
            do
            {
                candidate = Path.Combine(dir, $"{name}_{i}{ext}");
                i++;
            } while (File.Exists(candidate) && i < 10000);
            return candidate;
        }
    }
}
