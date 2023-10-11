using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FastRawSelector.CONTROL
{
    /// <summary>
    /// Interaction logic for SingleViewControl.xaml
    /// </summary>
    public partial class SingleViewControl : UserControl
    {
        //public IList<object> _objectItems = new ObservableCollection<object>();
        public SingleViewControl()
        {
            InitializeComponent();
            //MiniImgSp.ItemsSource = _objectItems;
        }

        public void SetImage(BitmapImage bmpImage)
        {
            MainImg.Source = bmpImage;
        }

        public void SetExif(ImageData data, int selectedCount = 0)
        {
            SetCount(data.Index, selectedCount);

            FileNameTb.Text = data.FileName;

            if (App.Setting.IsExifVisible)
            {
                StringBuilder exifText = new StringBuilder();
                if (data.IsRaw && data.Exif != null)
                {
                    foreach (var item in data.Exif)
                    {
                        //exifText.Append($"{item.Key} : {item.Value}\n");
                        if (!string.IsNullOrEmpty(item.Value.Item2))
                        {
                            switch (item.Value.Item1)
                            {
                                case "Image Height":
                                case "Image Width":
                                case "Flash bias":
                                case "White balance":
                                case "Camera model":
                                case "Aperture":
                                case "Exposure bias":
                                case "Exposure time":
                                case "Flash":
                                case "Focal length":
                                case "ISO speed":
                                case "Lens model":
                                    exifText.Append($"{item.Value.Item1} : {item.Value.Item2}\n");
                                    break;
                            }
                        }
                        //Log.Info($"{item.Key} : {item.Value}");
                    }
                }
                ExifTb.Text = exifText.ToString();
            }
        }

        public void SetCount(int index, int selectedCount = 0)
        {
            string count = (index + 1) + " / " + (LoadImage.LastIndex + 1);
            if (selectedCount != 0)
            {
                count += " (" + selectedCount + "장 선택) ";
            }
            CountTb.Text = count;
        }

        private void UserControl_MouseDown(object sender, MouseButtonEventArgs e)
        {
            MainImg.Focus();
        }

        public void SelectedUpdate(bool flg)
        {
            SelectedBd.Visibility = flg ? Visibility.Visible : Visibility.Collapsed;
            SelectCb.IsChecked = flg;
        }

        private void SelectCb_MouseUp(object sender, MouseButtonEventArgs e)
        {
            LoadImage.SelectEx();
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
            LoadImage.NextIamge();
        }

        //public void SetMiniImg()
        //{
        //    _objectItems.Clear();
        //    foreach (var data in LoadImage.ImageList)
        //    {
        //        try
        //        {
        //            BitmapImage bmpImage = new BitmapImage();
        //            using (MemoryStream buffer = new MemoryStream(data.Value.ImageArray))
        //            {
        //                if (buffer.Length == 0)
        //                {
        //                    return;
        //                }

        //                bmpImage.BeginInit();
        //                bmpImage.StreamSource = buffer;
        //                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
        //                bmpImage.EndInit();
        //            }
        //            //Image img = new Image();
        //            //img.Source = bmpImage;
        //            //img.Width = 100;
        //            //img.Height = 80;
        //            MiniImgItem item = new MiniImgItem()
        //            {
        //                Image = bmpImage,
        //                Title = data.Value.FileName
        //            };
        //            _objectItems.Add(item);
        //        }
        //        catch (System.Exception)
        //        {
        //        }

        //    }
        //}
    }
}
