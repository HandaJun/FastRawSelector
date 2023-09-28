using FastRawSelector.LOGIC;
using MetadataExtractor;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FastRawSelector.MANAGER
{
    public class BitmapManager
    {
        public static (byte[], List<MetadataExtractor.Directory>) GetBitmapArray(string path, double longEdge)
        {
            try
            {
                var directories = ImageMetadataReader.ReadMetadata(path).ToList();
                return (GetBytesFromImage(path, (int)longEdge), directories);
            }
            catch (Exception)
            {
                return (null, null);
            }
            
        }

        private static byte[] GetBytesFromImage(string imageFile, int longEdge)
        {
            MemoryStream ms = new MemoryStream();
            Size fullSize = default;
            using (Image img = Image.FromFile(imageFile))
            {
                fullSize = new Size(img.Width, img.Height);
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
            }

            using (var resizer = new ImageResizing(imageFile))
            {
                var resizedImage = resizer.Resize(fullSize.Width, fullSize.Height);
                var imageStream = resizedImage.ToStream();
                return imageStream.ToArray();
            }

        }


    }
}
