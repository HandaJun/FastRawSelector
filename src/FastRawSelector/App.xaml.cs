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
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("ko-KR");

            // 설정 읽어오기
            Setting = ApplicationSetting.Load();
            Setting.Save(false);

            Common.FileNameToRoot("log4net.dat");
            Common.FileNameToRoot("libraw.dll");
            if (Common.is64BitOperatingSystem)
            {
                Common.FileNameToRoot("exiv2-ql-64.dll");
            }
            else
            {
                Common.FileNameToRoot("exiv2-ql-32.dll");
            }

            new MainWindow(e.Args).Show();
        }
    }
}
