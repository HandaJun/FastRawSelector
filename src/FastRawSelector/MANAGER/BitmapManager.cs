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
    /// <summary>
    /// 일반 비트맵(JPEG/PNG 등) 디코드. MetadataExtractor + System.Drawing 리사이즈 후 JPEG byte[].
    /// AllowNotRawImage 가 켜진 폴더 로드 시 LoadImage 가 사용한다.
    /// </summary>
    public class BitmapManager
    {
        /// <summary>
        /// 장변 longEdge 로 축소한 JPEG byte[] 와 (선택) 메타데이터 디렉터리.
        /// loadExif=false 이면 리사이즈만 (프리패치 고속 경로, P-1).
        /// </summary>
        public static (byte[], List<MetadataExtractor.Directory>) GetBitmapArray(
            string path, double longEdge, bool loadExif = true)
        {
            try
            {
                List<MetadataExtractor.Directory> directories = null;
                if (loadExif)
                {
                    directories = ImageMetadataReader.ReadMetadata(path).ToList();
                }
                return (GetBytesFromImage(path, (int)longEdge), directories);
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(path, ex);
                return (null, null);
            }
        }

        /// <summary>메타데이터만 로드 (비트맵 EXIF 지연 로드용).</summary>
        public static List<MetadataExtractor.Directory> GetExifOnly(string path)
        {
            try
            {
                return ImageMetadataReader.ReadMetadata(path).ToList();
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(path, ex);
                return null;
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
