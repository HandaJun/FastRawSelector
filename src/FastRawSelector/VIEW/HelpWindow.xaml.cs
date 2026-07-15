using FastRawSelector.LOGIC;
using System.Windows;
using System.Windows.Input;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// 단축키·데이터 경로 안내 (모달). 설정 가능 키·언어는 현재 설정값을 표시.
    /// </summary>
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Title = Loc.Get("HelpTitle");
            FileTitleTb.Text = Loc.Get("HelpFile");
            NavTitleTb.Text = Loc.Get("HelpNav");
            SelectTitleTb.Text = Loc.Get("HelpSelectView");
            DataTitleTb.Text = Loc.Get("HelpData");
            DataBodyTb.Text = Loc.Get("HelpDataBody");
            NavBodyTb.Text = Loc.Get("HelpNavBody");
            FileKeysTb.Text = Loc.Get("HelpFileBody");
            CloseBt.Content = Loc.Get("Close");

            string sel = "B";
            string exif = "I";
            string full = "F";
            if (App.Setting != null)
            {
                sel = App.Setting.KeySelect ?? "B";
                exif = App.Setting.KeyExif ?? "I";
                full = App.Setting.KeyFullScreen ?? "F";
            }
            CustomKeysTb.Text = Loc.GetFormat("HelpCustomBody", sel, exif, full);
        }

        private void CloseBt_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape || e.Key == Key.F1)
            {
                Close();
            }
        }
    }
}
