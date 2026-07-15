using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FastRawSelector.CONTROL
{
    /// <summary>
    /// 풀스크린 뷰: 이미지·EXIF·선택. 공통 로직은 ImageViewHelper (Part 3).
    /// </summary>
    public partial class FullViewControl : UserControl
    {
        public FullViewControl()
        {
            InitializeComponent();
        }

        /// <summary>설정 단축키에 맞춰 ToolTip 갱신.</summary>
        public void RefreshHotkeyTooltips()
        {
            ImageViewHelper.RefreshHotkeyTooltips(SelectCb, ExifBt, PrevBt, NextBt);
        }

        /// <summary>테마 전환 후 아이콘/버튼 포그라운드 재연결.</summary>
        public void ApplyThemeChrome()
        {
            ImageViewHelper.ApplyThemeChrome(
                PrevBt, PrevIcon, NextBt, NextIcon, ExifBt, ExifIcon,
                SelectedBd, CountTb, FileNameTb);
        }

        public void SetImage(BitmapImage bmpImage)
        {
            MainImg.Source = bmpImage;
        }

        public void SetExif(ImageData data, int selectedCount = 0)
        {
            if (data == null)
            {
                return;
            }
            SetCount(data.Index, selectedCount);
            FileNameTb.Text = data.FileName;
            ExifTb.Text = ImageViewHelper.BuildExifText(data);
        }

        public void SetCount(int index, int selectedCount = 0)
        {
            CountTb.Text = ImageViewHelper.BuildCountText(index, selectedCount);
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainImg.Focus();
        }

        public void SelectedUpdate(bool flg)
        {
            ImageViewHelper.ApplySelectedState(SelectedBd, SelectCb, flg);
        }

        private void SelectCb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LoadImage.SelectEx();
        }

        private void TopBd_MouseEnter(object sender, MouseEventArgs e)
        {
            Common.Main.MenuBar.Visibility = Visibility.Visible;
        }

        private void MainImg_MouseEnter(object sender, MouseEventArgs e)
        {
            Common.Main.MenuBar.Visibility = Visibility.Collapsed;
        }

        private void ExifBt_Click(object sender, RoutedEventArgs e)
        {
            LoadImage.ChangeExif();
        }

        private void PrevBt_Click(object sender, RoutedEventArgs e)
        {
            LoadImage.PrevImage();
        }

        private void NextBt_Click(object sender, RoutedEventArgs e)
        {
            LoadImage.NextImage();
        }
    }
}
