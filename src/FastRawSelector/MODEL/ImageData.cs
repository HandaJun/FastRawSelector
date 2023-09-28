using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace FastRawSelector.MODEL
{
    public class ImageData
    {
        public int Index { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public bool IsNotImage { get; set; }

        public bool IsRaw { get; set; }
        public bool IsBitmap { get; set; }
        public byte[] ImageArray { get; set; }
        public SortedDictionary<string, (string, string)> Exif { get; set; } = null;
        public List<MetadataExtractor.Directory> BitmapExif { get; set; } = null;
    }
}
