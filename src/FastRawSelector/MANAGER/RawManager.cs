using FastRawSelector.LOGIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastRawSelector.MANAGER
{
    public class RawManager
    {
        public static (BitmapSource, SortedDictionary<string, (string, string)>) GetThumbnail(string path, double longEdge)
        {
            MetaProvider Meta = new MetaProvider(path);

            var fullSize = Meta.GetSize();
            var orientation = Meta.GetOrientation();

            try
            {
                using (var buffer = new MemoryStream(Meta.GetThumbnail()))
                {
                    if (buffer.Length == 0)
                        return (null, null);

                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = buffer;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();

                    if (fullSize.Width > fullSize.Height && fullSize.Width > longEdge)
                    {
                        fullSize.Height = fullSize.Height * longEdge / fullSize.Width;
                        fullSize.Width = longEdge;
                    }
                    else if (fullSize.Width < fullSize.Height && fullSize.Height > longEdge)
                    {
                        fullSize.Width = fullSize.Width * longEdge / fullSize.Height;
                        fullSize.Height = longEdge;
                    }

                    var transformed = RotateAndScaleThumbnail(img, orientation, fullSize);
                    transformed.Freeze();
                    return (transformed, Meta.GetExif());
                }
            }
            catch (Exception)
            {
                return (null, null);
            }
            finally
            {
            }
        }

        public static (byte[], SortedDictionary<string, (string, string)>) GetThumbnailArray(string path, double longEdge)
        {
            MetaProvider Meta = new MetaProvider(path);

            var fullSize = Meta.GetSize();
            var orientation = Meta.GetOrientation();

            try
            {
                using (var buffer = new MemoryStream(Meta.GetThumbnail()))
                {
                    if (buffer.Length == 0)
                        return (null, null);

                    var img = new BitmapImage();
                    img.BeginInit();
                    img.StreamSource = buffer;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();

                    if (fullSize.Width > fullSize.Height && fullSize.Width > longEdge)
                    {
                        fullSize.Height = fullSize.Height * longEdge / fullSize.Width;
                        fullSize.Width = longEdge;
                    }
                    else if (fullSize.Width < fullSize.Height && fullSize.Height > longEdge)
                    {
                        fullSize.Width = fullSize.Width * longEdge / fullSize.Height;
                        fullSize.Height = longEdge;
                    }

                    var transformed = RotateAndScaleThumbnail(img, orientation, fullSize);
                    transformed.Freeze();

                    byte[] data;
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(transformed));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        data = ms.ToArray();
                    }

                    return (data, Meta.GetExif());
                }
            }
            catch (Exception)
            {
                return (null, null);
            }
            finally
            {
            }
        }
        private static TransformedBitmap RotateAndScaleThumbnail(BitmapImage image, Orientation orientation,
            System.Windows.Size fullSize)
        {
            var swap = false;

            var transforms = new TransformGroup();

            // some RAWs, like from RX100, have thumbnails already rotated.
            if (fullSize.Height >= fullSize.Width && image.PixelHeight <= image.PixelWidth ||
                fullSize.Height < fullSize.Width && image.PixelHeight > image.PixelWidth)
                switch (orientation)
                {
                    case Orientation.TopRight:
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        break;
                    case Orientation.BottomRight:
                        transforms.Children.Add(new RotateTransform(180));
                        break;
                    case Orientation.BottomLeft:
                        transforms.Children.Add(new ScaleTransform(1, 1, 0, 0));
                        break;
                    case Orientation.LeftTop:
                        transforms.Children.Add(new RotateTransform(90));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swap = true;
                        break;
                    case Orientation.RightTop:
                        transforms.Children.Add(new RotateTransform(90));
                        swap = true;
                        break;
                    case Orientation.RightBottom:
                        transforms.Children.Add(new RotateTransform(270));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swap = true;
                        break;
                    case Orientation.LeftBottom:
                        transforms.Children.Add(new RotateTransform(270));
                        swap = true;
                        break;
                }

            transforms.Children.Add(swap
                ? new ScaleTransform(fullSize.Width / image.PixelHeight, fullSize.Height / image.PixelWidth)
                : new ScaleTransform(fullSize.Width / image.PixelWidth, fullSize.Height / image.PixelHeight));

            return new TransformedBitmap(image, transforms);
        }

        public static SortedDictionary<string, (string, string)> RawToJpeg(string path, int size, string outputPath)
        {
            var thumbnail = GetThumbnail(path, size);
            if (thumbnail.Item1 == null)
            {
                return null;
            }
            if (File.Exists(outputPath))
            {
                return thumbnail.Item2;
            }

            using (var fileStream = new FileStream(outputPath, FileMode.Create))
            {
                BitmapEncoder encoder = new JpegBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(thumbnail.Item1));
                encoder.Save(fileStream);
                return thumbnail.Item2;
            }
        }
    }
}
