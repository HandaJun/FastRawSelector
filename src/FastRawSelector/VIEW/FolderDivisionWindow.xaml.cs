using FastRawSelector.LOGIC;
using System.Windows;
using System.Windows.Controls;

namespace FastRawSelector.VIEW
{
    /// <summary>
    /// Interaction logic for FolderDivisionWindow.xaml
    /// </summary>
    public partial class FolderDivisionWindow : Window
    {
        private new bool IsLoaded = false;
        private string TargetPath = null;

        private readonly static FolderDivisionWindow _instance = new FolderDivisionWindow();
        public static FolderDivisionWindow GetInstance()
        {
            return _instance;
        }
        public FolderDivisionWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

        public void Init()
        {
            //ExportPathTb.Text = LoadImage.NowDir;
            //if (LoadImage.NowDir == null)
            //{
            //    AllCbi.IsEnabled = false;
            //    SelectedCbi.IsEnabled = false;
            //    ExportTargetCb.SelectedIndex = 2;
            //    RawPathTb.IsEnabled = true;
            //    RawPathOpenBt.IsEnabled = true;
            //    SelectedCbi.Content = $"선택한 사진만";
            //    RawPathTb.Focus();
            //}
            //else
            //{
            //    ExportTargetCb.SelectedIndex = 0;
            //    AllCbi.IsEnabled = true;
            //    int selectedCount = 0;
            //    if (Common.NowSelectorSetting != null)
            //    {
            //        selectedCount = Common.NowSelectorSetting.SelectedSet.Count;
            //    }
            //    SelectedCbi.Content = $"선택한 사진만 ({selectedCount}장 선택중)";
            //    if (selectedCount == 0)
            //    {
            //        SelectedCbi.IsEnabled = false;
            //        RawPathTb.Focus();
            //    }
            //    else
            //    {
            //        SelectedCbi.IsEnabled = true;
            //        ExportTargetCb.SelectedIndex = 1;
            //        ExportPathTb.Focus();
            //    }
            //}
        }

        public void ShowWindow(Window owner)
        {
            Common.MoveCenter(owner, this);
            IsLoaded = false;
            Show();
            Init();
            BringToFront();
            IsLoaded = true;
        }

        private void TargetPathOpenBt_Click(object sender, RoutedEventArgs e)
        {
            Common.GetFolder("분류할 폴더", TargetPathTb.Text, f =>
            {
                TargetPathTb.Text = f;
            });
            BringToFront();
        }

        private void WindowAlert(string msg, bool closeFlg = false)
        {
            Common.Invoke(() =>
            {
                ExportPb.Visibility = Visibility.Collapsed;
                _ = Alert.Info(msg, afterAct: () =>
                {
                    if (closeFlg)
                    {
                        Close();
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

        }
    }
}
