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
        /// <summary>연속 디코드 실패 시 영구 스킵까지의 허용 횟수 (P-4).</summary>
        public const int MaxDecodeFail = 3;

        public int Index { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        /// <summary>MaxDecodeFail 회 실패 후 true. 일시 실패는 DecodeFailCount 만 증가.</summary>
        public bool IsNotImage { get; set; }
        /// <summary>디코드 실패 누적. 성공 시 0으로 리셋 (P-4).</summary>
        public int DecodeFailCount { get; set; }

        public bool IsRaw { get; set; }
        public bool IsBitmap { get; set; }
        /// <summary>
        /// 표시용 JPEG 바이트. null 이면 미로드 또는 메모리 상한으로 해제됨 (Part 1).
        /// 해제만으로 IsNotImage 가 되지 않음 — 재진입 시 재디코드.
        /// </summary>
        public byte[] ImageArray { get; set; }
        public SortedDictionary<string, (string, string)> Exif { get; set; } = null;
        public List<MetadataExtractor.Directory> BitmapExif { get; set; } = null;
    }
}
