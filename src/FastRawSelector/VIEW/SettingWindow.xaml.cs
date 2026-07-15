using FastRawSelector.LOGIC;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// 앱 전역 설정 (모달 OK/취소).
    /// 탭: 일반 / 화면 / 단축키 / 탐색기 / 로그.
    /// </summary>
    public partial class SettingWindow : Window
    {
        private static readonly string[] LetterKeys = new[]
        {
            "A","B","C","D","E","F","G","H","I","J","K","L","M",
            "N","O","P","Q","R","S","T","U","V","W","X","Y","Z"
        };

        public SettingWindow()
        {
            InitializeComponent();
            FillKeyCombo(KeySelectCb);
            FillKeyCombo(KeyExifCb);
            FillKeyCombo(KeyFullScreenCb);
            LoadFromSetting();
        }

        private static void FillKeyCombo(ComboBox cb)
        {
            cb.Items.Clear();
            foreach (var k in LetterKeys)
            {
                cb.Items.Add(k);
            }
        }

        private void LoadFromSetting()
        {
            var s = App.Setting;
            if (s == null)
            {
                return;
            }

            AlwaysOnTopCb.IsChecked = s.AlwaysOnTop;
            IsExifVisibleCb.IsChecked = s.IsExifVisible;
            AllowNotRawImageCb.IsChecked = s.AllowNotRawImage;
            OpenLastFolderCb.IsChecked = s.OpenLastFolderOnStartup;
            AutoCheckUpdateCb.IsChecked = s.AutoCheckUpdate;
            SendToCb.IsChecked = ShellIntegration.IsSendToRegistered();
            OpenWithCb.IsChecked = ShellIntegration.IsOpenWithRegistered();

            SelectComboByTag(ThemeCb, string.IsNullOrEmpty(s.Theme) ? "Dark" : s.Theme);
            SelectComboByTag(LogLevelCb, string.IsNullOrEmpty(s.LogLevel) ? "INFO" : s.LogLevel);
            SelectComboByTag(LanguageCb, string.IsNullOrEmpty(s.Language) ? "ko" : s.Language);
            SelectKeyCombo(KeySelectCb, s.KeySelect, "B");
            SelectKeyCombo(KeyExifCb, s.KeyExif, "I");
            SelectKeyCombo(KeyFullScreenCb, s.KeyFullScreen, "F");

            if (!string.IsNullOrEmpty(s.LastFolderPath))
            {
                LastFolderTb.Text = Loc.GetFormat("LastFolder", s.LastFolderPath);
            }
            else
            {
                LastFolderTb.Text = Loc.Get("LastFolderNone");
            }
        }

        private static void SelectComboByTag(ComboBox cb, string tag)
        {
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] is ComboBoxItem item && item.Tag != null
                    && item.Tag.ToString().Equals(tag, System.StringComparison.OrdinalIgnoreCase))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
            if (cb.Items.Count > 0)
            {
                cb.SelectedIndex = 0;
            }
        }

        private static void SelectKeyCombo(ComboBox cb, string key, string fallback)
        {
            string k = string.IsNullOrEmpty(key) ? fallback : key.ToUpperInvariant();
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] != null && cb.Items[i].ToString().Equals(k, System.StringComparison.OrdinalIgnoreCase))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
            for (int i = 0; i < cb.Items.Count; i++)
            {
                if (cb.Items[i] != null && cb.Items[i].ToString().Equals(fallback, System.StringComparison.OrdinalIgnoreCase))
                {
                    cb.SelectedIndex = i;
                    return;
                }
            }
            if (cb.Items.Count > 0)
            {
                cb.SelectedIndex = 0;
            }
        }

        private static string GetTag(ComboBox cb, string fallback)
        {
            if (cb.SelectedItem is ComboBoxItem cbi && cbi.Tag != null)
            {
                return cbi.Tag.ToString();
            }
            return fallback;
        }

        private static string GetKeySelection(ComboBox cb, string fallback)
        {
            if (cb.SelectedItem != null)
            {
                return cb.SelectedItem.ToString();
            }
            return fallback;
        }

        private void OkBt_Click(object sender, RoutedEventArgs e)
        {
            if (App.Setting == null)
            {
                DialogResult = false;
                return;
            }

            string logLevel = GetTag(LogLevelCb, "INFO");
            string theme = GetTag(ThemeCb, "Dark");
            string language = GetTag(LanguageCb, "ko");
            string keySelect = GetKeySelection(KeySelectCb, "B");
            string keyExif = GetKeySelection(KeyExifCb, "I");
            string keyFull = GetKeySelection(KeyFullScreenCb, "F");

            // 동일 키 중복 방지
            if (string.Equals(keySelect, keyExif, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(keySelect, keyFull, System.StringComparison.OrdinalIgnoreCase)
                || string.Equals(keyExif, keyFull, System.StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show(Loc.Get("HotkeyConflict"), Loc.Get("SettingsTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            App.Setting.AlwaysOnTop = AlwaysOnTopCb.IsChecked == true;
            App.Setting.IsExifVisible = IsExifVisibleCb.IsChecked == true;
            App.Setting.AllowNotRawImage = AllowNotRawImageCb.IsChecked == true;
            App.Setting.OpenLastFolderOnStartup = OpenLastFolderCb.IsChecked == true;
            App.Setting.AutoCheckUpdate = AutoCheckUpdateCb.IsChecked == true;
            App.Setting.LogLevel = logLevel;
            App.Setting.Theme = theme;
            App.Setting.Language = language;
            App.Setting.KeySelect = keySelect;
            App.Setting.KeyExif = keyExif;
            App.Setting.KeyFullScreen = keyFull;

            try
            {
                App.Setting.Save();
                Log.ApplyRootLevel(App.Setting.LogLevel);
                Loc.ApplyLanguage(App.Setting.Language);
                Common.ApplyTheme(App.Setting.Theme);

                if (Common.Main != null)
                {
                    Common.Main.Topmost = App.Setting.AlwaysOnTop;
                    Common.Main.RefreshLanguage();
                }

                LoadImage.ChangeExif(App.Setting.IsExifVisible);
                if (App.Setting.IsExifVisible && LoadImage.NowImage != null)
                {
                    LoadImage.SetExif(LoadImage.NowImage);
                }

                // D-2: 탐색기 연동 (opt-in, 현재 상태와 다르면 적용)
                try
                {
                    bool wantSendTo = SendToCb.IsChecked == true;
                    if (wantSendTo != ShellIntegration.IsSendToRegistered())
                    {
                        ShellIntegration.SetSendToRegistered(wantSendTo);
                    }
                    bool wantOpenWith = OpenWithCb.IsChecked == true;
                    if (wantOpenWith != ShellIntegration.IsOpenWithRegistered())
                    {
                        ShellIntegration.SetOpenWithRegistered(wantOpenWith);
                    }
                }
                catch (System.Exception shellEx)
                {
                    Log.Exception(shellEx);
                    MessageBox.Show(
                        Loc.GetFormat("ShellError", shellEx.Message),
                        Loc.Get("SettingsTitle"), MessageBoxButton.OK, MessageBoxImage.Warning);
                }

                Log.Info($"설정 저장: Theme={App.Setting.Theme}, Lang={App.Setting.Language}, AlwaysOnTop={App.Setting.AlwaysOnTop}, OpenLastFolder={App.Setting.OpenLastFolderOnStartup}, AutoCheckUpdate={App.Setting.AutoCheckUpdate}, Keys={App.Setting.KeySelect}/{App.Setting.KeyExif}/{App.Setting.KeyFullScreen}, LogLevel={App.Setting.LogLevel}, SendTo={SendToCb.IsChecked}, OpenWith={OpenWithCb.IsChecked}");
            }
            catch (System.Exception ex)
            {
                Log.Exception(ex);
            }

            DialogResult = true;
        }

        private void CheckUpdateNowBt_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdateNowBt.IsEnabled = false;
            try
            {
                // 확인 버튼 직후 설정 반영 전이어도 즉시 검사
                UpdateChecker.CheckAsync(silentIfUpToDate: false, showErrors: true);
            }
            finally
            {
                // 비동기라 바로 다시 켤 수 있음 — 연속 클릭 방지 짧게
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    CheckUpdateNowBt.IsEnabled = true;
                }), System.Windows.Threading.DispatcherPriority.Background);
            }
        }

        private void CancelBt_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }
    }
}
