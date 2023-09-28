using MahApps.Metro.IconPacks;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace FastRawSelector.CONTROL
{
    #region HeaderToImageConverter

    [ValueConversion(typeof(string), typeof(bool))]
    public class HeaderToImageConverter : IValueConverter
    {
        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((value as string).Contains(@"\"))
            {
                var packIconMaterial = new PackIconBoxIcons()
                {
                    Kind = PackIconBoxIconsKind.RegularFolder,
                    Width = 20,
                    Height = 20,
                };
                return packIconMaterial;

                //Uri uri = new Uri("pack://application:,,,/img/diskdrive.png");
                //BitmapImage source = new BitmapImage(uri);
                //return source;
            }
            else
            {
                var packIconMaterial = new PackIconBoxIcons()
                {
                    Kind = PackIconBoxIconsKind.RegularFolder,
                    Width = 20,
                    Height = 20,
                };
                return packIconMaterial;
                //Uri uri = new Uri("pack://application:,,,/img/folder.png");
                //BitmapImage source = new BitmapImage(uri);
                //return source;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    #endregion // DoubleToIntegerConverter


}