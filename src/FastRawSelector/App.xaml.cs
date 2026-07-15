using FastRawSelector.LOGIC;
using System.Globalization;
using System.Windows;

namespace FastRawSelector
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ApplicationSetting Setting { get; set; } // 설정
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            // 설정·로그·네이티브 DLL → %AppData%\Roaming\FastRawSelector
            Common.InitNativeRuntime();

            // 설정 읽어오기 + 로그 레벨·테마·언어 적용
            Setting = ApplicationSetting.Load();
            Log.ApplyRootLevel(Setting.LogLevel);
            Loc.ApplyLanguage(Setting.Language);
            Common.ApplyTheme(Setting.Theme);
            Setting.Save(false);

            Log.Info($"앱 기동 완료. LogLevel={Setting.LogLevel}, Theme={Setting.Theme}, Lang={Setting.Language}, OpenLastFolder={Setting.OpenLastFolderOnStartup}, Args={e.Args?.Length ?? 0}");

            new MainWindow(e.Args).Show();
        }
    }
}
