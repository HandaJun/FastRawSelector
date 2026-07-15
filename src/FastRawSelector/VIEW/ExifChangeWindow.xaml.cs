using FastRawSelector.LOGIC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// 파일 생성/수정 시각 일괄 변경 (미리보기 → 확인 → 적용).
    /// EXIF 태그 쓰기는 미포함 (읽기 전용 바인딩). EXIF 촬영 시각으로 맞추기만 지원.
    /// hide-on-close 싱글톤.
    /// </summary>
    public partial class ExifChangeWindow : Window
    {
        private bool IsWindowReady = false;
        private readonly ObservableCollection<TimePreviewItem> _preview = new ObservableCollection<TimePreviewItem>();

        private readonly static ExifChangeWindow _instance = new ExifChangeWindow();
        public static ExifChangeWindow GetInstance()
        {
            return _instance;
        }

        private ExifChangeWindow()
        {
            InitializeComponent();
            PreviewLv.ItemsSource = _preview;
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
            _preview.Clear();
            ApplyPb.Value = 0;
            ApplyPb.Visibility = Visibility.Collapsed;
            ApplyBt.IsEnabled = true;

            TargetDatePicker.SelectedDate = DateTime.Today;
            HourTb.Text = "12";
            MinuteTb.Text = "0";
            OffsetDaysTb.Text = "0";
            OffsetHoursTb.Text = "0";
            ModeAbsoluteRb.IsChecked = true;
            UpdateModePanels();

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
            }
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

        private void Mode_Checked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }
            UpdateModePanels();
        }

        private void UpdateModePanels()
        {
            if (AbsolutePanel == null || OffsetPanel == null)
            {
                return;
            }
            bool abs = ModeAbsoluteRb != null && ModeAbsoluteRb.IsChecked == true;
            bool off = ModeOffsetRb != null && ModeOffsetRb.IsChecked == true;
            AbsolutePanel.Visibility = abs ? Visibility.Visible : Visibility.Collapsed;
            OffsetPanel.Visibility = off ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SourcePathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder(Loc.Get("FolderPickerTarget"), SourcePathTb.Text, f =>
            {
                SourcePathTb.Text = f;
            });
            BringToFront();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tb)
            {
                tb.ToolTip = tb.Text;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            Hide();
        }

        public void BringToFront()
        {
            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void WindowAlert(string msg)
        {
            Common.Invoke(() =>
            {
                ApplyPb.Visibility = Visibility.Collapsed;
                ApplyBt.IsEnabled = true;
                _ = Alert.Info(msg, afterAct: BringToFront);
            });
        }

        private void PreviewBt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var files = CollectSourceFiles();
                if (files.Count == 0)
                {
                    WindowAlert(Loc.Get("FileTimeNoFiles"));
                    return;
                }

                if (!TryBuildTargetTime(out DateTime? absolute, out TimeSpan? offset, out bool fromExif, out string err))
                {
                    WindowAlert(err);
                    return;
                }

                bool applyCreated = ApplyCreatedCb.IsChecked == true;
                bool applyModified = ApplyModifiedCb.IsChecked == true;
                if (!applyCreated && !applyModified)
                {
                    WindowAlert(Loc.Get("FileTimeNeedTimestamp"));
                    return;
                }

                _preview.Clear();
                int ok = 0;
                int skip = 0;
                foreach (var file in files)
                {
                    var item = BuildPreviewItem(file, absolute, offset, fromExif, applyCreated, applyModified);
                    _preview.Add(item);
                    if (item.CanApply)
                    {
                        ok++;
                    }
                    else
                    {
                        skip++;
                    }
                }

                Log.Info($"파일시간 미리보기: total={files.Count}, ok={ok}, skip={skip}");
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
                WindowAlert(Loc.GetFormat("FileTimePreviewFail", ex.Message));
            }
        }

        private void ApplyBt_Click(object sender, RoutedEventArgs e)
        {
            if (_preview.Count == 0)
            {
                WindowAlert(Loc.Get("FileTimeNeedPreview"));
                return;
            }

            var toApply = _preview.Where(p => p.CanApply).ToList();
            if (toApply.Count == 0)
            {
                WindowAlert(Loc.Get("FileTimeNothing"));
                return;
            }

            bool applyCreated = ApplyCreatedCb.IsChecked == true;
            bool applyModified = ApplyModifiedCb.IsChecked == true;

            var confirm = MessageBox.Show(
                Loc.GetFormat("FileTimeConfirm", toApply.Count),
                Loc.Get("FileTimeTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes)
            {
                return;
            }

            ApplyBt.IsEnabled = false;
            ApplyPb.Visibility = Visibility.Visible;
            ApplyPb.Value = 0;

            Log.Info($"파일시간 적용 시작: count={toApply.Count}, created={applyCreated}, modified={applyModified}");

            Task.Run(() =>
            {
                int ok = 0;
                int fail = 0;
                int total = toApply.Count;
                int done = 0;

                foreach (var item in toApply)
                {
                    try
                    {
                        if (!File.Exists(item.FullPath))
                        {
                            throw new FileNotFoundException(item.FullPath);
                        }
                        if (applyCreated)
                        {
                            File.SetCreationTime(item.FullPath, item.NewTime);
                        }
                        if (applyModified)
                        {
                            File.SetLastWriteTime(item.FullPath, item.NewTime);
                        }
                        // 접근 시간도 맞춤 (선택)
                        try
                        {
                            File.SetLastAccessTime(item.FullPath, item.NewTime);
                        }
                        catch
                        {
                        }
                        Interlocked.Increment(ref ok);
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref fail);
                        Log.ExceptionWithMsg(item.FullPath, ex);
                    }

                    int d = Interlocked.Increment(ref done);
                    double pct = total > 0 ? Math.Min(100d, (double)d / total * 100d) : 100d;
                    Common.Invoke(() => { ApplyPb.Value = pct; });
                }

                Log.Info($"파일시간 적용 완료: ok={ok}, fail={fail}");
                WindowAlert(Loc.GetFormat("FileTimeDone", ok, fail));
                Common.Invoke(() =>
                {
                    // 미리보기 갱신
                    PreviewBt_Click(null, null);
                });
            });
        }

        private List<string> CollectSourceFiles()
        {
            string sourceMode = "All";
            if (SourceTargetCb.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
            {
                sourceMode = cbi.Tag.ToString();
            }
            string sourcePath = SourcePathTb.Text;

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

        private bool TryBuildTargetTime(out DateTime? absolute, out TimeSpan? offset, out bool fromExif, out string error)
        {
            absolute = null;
            offset = null;
            fromExif = false;
            error = null;

            if (ModeAbsoluteRb.IsChecked == true)
            {
                if (TargetDatePicker.SelectedDate == null)
                {
                    error = Loc.Get("FileTimeNeedDate");
                    return false;
                }
                if (!int.TryParse(HourTb.Text, out int h) || h < 0 || h > 23)
                {
                    error = Loc.Get("FileTimeHourRange");
                    return false;
                }
                if (!int.TryParse(MinuteTb.Text, out int m) || m < 0 || m > 59)
                {
                    error = Loc.Get("FileTimeMinRange");
                    return false;
                }
                absolute = TargetDatePicker.SelectedDate.Value.Date.AddHours(h).AddMinutes(m);
                return true;
            }
            if (ModeOffsetRb.IsChecked == true)
            {
                if (!int.TryParse(OffsetDaysTb.Text, out int days))
                {
                    error = Loc.Get("FileTimeDayNum");
                    return false;
                }
                if (!int.TryParse(OffsetHoursTb.Text, out int hours))
                {
                    error = Loc.Get("FileTimeHourNum");
                    return false;
                }
                offset = TimeSpan.FromDays(days) + TimeSpan.FromHours(hours);
                return true;
            }
            if (ModeExifRb.IsChecked == true)
            {
                fromExif = true;
                return true;
            }
            error = Loc.Get("FileTimeNeedMode");
            return false;
        }

        private static TimePreviewItem BuildPreviewItem(
            string file,
            DateTime? absolute,
            TimeSpan? offset,
            bool fromExif,
            bool applyCreated,
            bool applyModified)
        {
            var fi = new FileInfo(file);
            DateTime beforeCreated = fi.CreationTime;
            DateTime beforeModified = fi.LastWriteTime;
            DateTime newTime;
            string note = "";

            if (absolute.HasValue)
            {
                newTime = absolute.Value;
                note = Loc.Get("FileTimeNoteAbsolute");
            }
            else if (offset.HasValue)
            {
                // 기준: 수정 시간 (없으면 생성)
                newTime = beforeModified.Add(offset.Value);
                note = Loc.GetFormat("FileTimeNoteOffset", FormatOffset(offset.Value));
            }
            else if (fromExif)
            {
                DateTime? exif = TryGetExifDateTime(file);
                if (exif.HasValue)
                {
                    newTime = exif.Value;
                    note = "EXIF";
                }
                else
                {
                    return new TimePreviewItem
                    {
                        FullPath = file,
                        FileName = Path.GetFileName(file),
                        BeforeModified = beforeModified.ToString("yyyy-MM-dd HH:mm"),
                        After = "-",
                        Note = Loc.Get("FileTimeNoExif"),
                        NewTime = beforeModified,
                        CanApply = false
                    };
                }
            }
            else
            {
                newTime = beforeModified;
                note = "?";
            }

            return new TimePreviewItem
            {
                FullPath = file,
                FileName = Path.GetFileName(file),
                BeforeModified = string.Format(
                    CultureInfo.InvariantCulture,
                    "C:{0:MM-dd HH:mm} M:{1:MM-dd HH:mm}",
                    beforeCreated, beforeModified),
                After = newTime.ToString("yyyy-MM-dd HH:mm"),
                Note = note + (applyCreated && applyModified ? "" : applyCreated ? Loc.Get("FileTimeCreatedOnly") : Loc.Get("FileTimeModifiedOnly")),
                NewTime = newTime,
                CanApply = true
            };
        }

        private static string FormatOffset(TimeSpan ts)
        {
            string sign = ts < TimeSpan.Zero ? "-" : "+";
            var abs = ts.Duration();
            return string.Format(CultureInfo.InvariantCulture, "{0}{1}d {2}h", sign, abs.Days, abs.Hours);
        }

        /// <summary>EXIF 촬영 시각 파싱 시도.</summary>
        private static DateTime? TryGetExifDateTime(string filePath)
        {
            try
            {
                SortedDictionary<string, (string, string)> exif = null;
                if (LoadImage.ImageList != null && LoadImage.ImageList.ContainsKey(filePath))
                {
                    exif = LoadImage.ImageList[filePath].Exif;
                }
                if ((exif == null || exif.Count == 0) && Common.IsRawFile(filePath))
                {
                    var meta = new MetaProvider(filePath);
                    exif = meta.GetExif();
                }
                if (exif == null)
                {
                    return null;
                }

                string[] labels =
                {
                    "Date and Time (Original)",
                    "Date and Time",
                    "DateTimeOriginal",
                    "DateTime",
                    "Date/Time Original",
                    "Create Date"
                };

                foreach (var kv in exif)
                {
                    string label = kv.Value.Item1 ?? "";
                    string value = kv.Value.Item2 ?? "";
                    foreach (var want in labels)
                    {
                        if (label.IndexOf(want, StringComparison.OrdinalIgnoreCase) >= 0
                            || (kv.Key != null && kv.Key.IndexOf("Date", StringComparison.OrdinalIgnoreCase) >= 0
                                && kv.Key.IndexOf("Time", StringComparison.OrdinalIgnoreCase) >= 0))
                        {
                            DateTime dt;
                            if (TryParseExifDate(value, out dt))
                            {
                                return dt;
                            }
                        }
                    }
                }

                // 값만 훑기
                foreach (var kv in exif)
                {
                    DateTime dt;
                    if (TryParseExifDate(kv.Value.Item2, out dt))
                    {
                        // 연도가 합리적이면 채택
                        if (dt.Year >= 1980 && dt.Year <= 2100)
                        {
                            return dt;
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

        private static bool TryParseExifDate(string value, out DateTime dt)
        {
            dt = default(DateTime);
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }
            value = value.Trim();
            // 일반 EXIF: "yyyy:MM:dd HH:mm:ss"
            string[] formats =
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "yyyy:MM:dd HH:mm",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-dd"
            };
            if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeLocal, out dt))
            {
                return true;
            }
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out dt);
        }

        /// <summary>미리보기 행 모델.</summary>
        private class TimePreviewItem
        {
            public string FullPath { get; set; }
            public string FileName { get; set; }
            public string BeforeModified { get; set; }
            public string After { get; set; }
            public string Note { get; set; }
            public DateTime NewTime { get; set; }
            public bool CanApply { get; set; }
        }
    }
}
