using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Media.Imaging;

namespace FastRawSelector.LOGIC
{
    /// <summary>
    /// 그리드 전용 썸네일 디스크 캐시.
    /// %AppData%\Roaming\FastRawSelector\thumbs\
    /// UI 칸 크기에 맞춰 디코드 폭을 고르되 MaxDecodeWidth 로 메모리 상한.
    /// </summary>
    public static class GridThumbCache
    {
        /// <summary>
        /// 그리드 디코드 최대 한 변(px).
        /// 256→640: 가상화 유지하면서 표시 품질 개선. 뷰포트만 로드하므로 메모리 허용.
        /// </summary>
        public const int MaxDecodeWidth = 640;

        /// <summary>최소 디코드 폭 (너무 작으면 확대 시 심하게 뭉개짐).</summary>
        public const int MinDecodeWidth = 160;

        public static readonly string ThumbsDir = Path.Combine(Common.AppDataPath, "thumbs");

        /// <summary>칸 크기(size)에 맞는 디코드 폭. UI 슬라이더와 연동하되 상한 적용.</summary>
        public static int GetDecodeWidth(int cellSize)
        {
            if (cellSize < MinDecodeWidth)
            {
                return MinDecodeWidth;
            }
            if (cellSize > MaxDecodeWidth)
            {
                return MaxDecodeWidth;
            }
            return cellSize;
        }

        public static string GetCachePath(string imagePath, int decodeWidth)
        {
            try
            {
                var fi = new FileInfo(imagePath);
                if (!fi.Exists)
                {
                    return null;
                }
                // v3: 회전 보정 로직 변경 후 구 캐시 무효화
                string key = imagePath + "|" + fi.Length + "|" + fi.LastWriteTimeUtc.Ticks + "|w" + decodeWidth + "|v3";
                string hash = Hash(key);
                return Path.Combine(ThumbsDir, hash + ".jpg");
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 캐시 또는 ImageArray/파일에서 BitmapImage 생성.
        /// decodeWidth 로 다운스케일 (원본 1616 JPEG에서 고품질 축소).
        /// </summary>
        public static BitmapImage LoadThumb(string imagePath, byte[] imageArray, int decodeWidth)
        {
            try
            {
                decodeWidth = GetDecodeWidth(decodeWidth);

                if (!Directory.Exists(ThumbsDir))
                {
                    Directory.CreateDirectory(ThumbsDir);
                }

                string cachePath = GetCachePath(imagePath, decodeWidth);
                if (!string.IsNullOrEmpty(cachePath) && File.Exists(cachePath))
                {
                    // 캐시는 이미 목표 크기 JPEG → 추가 축소 없이 로드
                    return LoadBitmapFromFile(cachePath, 0);
                }

                BitmapImage bmp = null;
                // ImageArray(싱글용 고품질 JPEG) 우선 — 파일 재읽기보다 선명
                if (imageArray != null && imageArray.Length > 0)
                {
                    bmp = LoadBitmapFromBytes(imageArray, decodeWidth);
                }
                else if (File.Exists(imagePath) && CanWpfOpenDirect(imagePath))
                {
                    // RAW 는 WPF 가 못 읽음 — 예외·대기 없이 스킵 (GridThumbLoader 가 경량 디코드)
                    bmp = LoadBitmapFromFile(imagePath, decodeWidth);
                }

                if (bmp != null && !string.IsNullOrEmpty(cachePath))
                {
                    TrySaveCache(bmp, cachePath);
                }
                return bmp;
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(imagePath, ex);
                return null;
            }
        }

        /// <summary>
        /// 그리드 전용: 이미 뽑은 JPEG 바이트를 디스크 캐시에 쓰고 BitmapImage 로 반환.
        /// </summary>
        public static BitmapImage SaveAndLoad(string imagePath, byte[] jpegBytes, int decodeWidth)
        {
            if (string.IsNullOrEmpty(imagePath) || jpegBytes == null || jpegBytes.Length == 0)
            {
                return null;
            }
            try
            {
                decodeWidth = GetDecodeWidth(decodeWidth);
                if (!Directory.Exists(ThumbsDir))
                {
                    Directory.CreateDirectory(ThumbsDir);
                }

                // jpegBytes 가 이미 목표 장변이면 DecodePixelWidth 0 으로 그대로 로드
                BitmapImage bmp = LoadBitmapFromBytes(jpegBytes, 0);

                string cachePath = GetCachePath(imagePath, decodeWidth);
                if (bmp != null && !string.IsNullOrEmpty(cachePath) && !File.Exists(cachePath))
                {
                    try
                    {
                        File.WriteAllBytes(cachePath, jpegBytes);
                    }
                    catch
                    {
                        TrySaveCache(bmp, cachePath);
                    }
                }
                return bmp;
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(imagePath, ex);
                return null;
            }
        }

        /// <summary>WPF BitmapImage 가 직접 열 수 있는 확장자만 true (RAW 제외).</summary>
        private static bool CanWpfOpenDirect(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                return false;
            }
            // RAW 는 실패가 느릴 수 있어 시도하지 않음
            if (Common.IsRawFile(imagePath))
            {
                return false;
            }
            return Common.IsBitmapFile(imagePath);
        }

        /// <param name="decodeW">0 이면 DecodePixelWidth 미지정(캐시 파일용).</param>
        private static BitmapImage LoadBitmapFromBytes(byte[] data, int decodeW)
        {
            var bmp = new BitmapImage();
            using (var ms = new MemoryStream(data))
            {
                bmp.BeginInit();
                bmp.StreamSource = ms;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                if (decodeW > 0)
                {
                    bmp.DecodePixelWidth = decodeW;
                }
                bmp.EndInit();
            }
            bmp.Freeze();
            return bmp;
        }

        private static BitmapImage LoadBitmapFromFile(string path, int decodeW)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = new Uri(path, UriKind.Absolute);
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            // 색 프로필 유지로 색감 개선 (무시 시 약간 탁해질 수 있음)
            bmp.CreateOptions = BitmapCreateOptions.None;
            if (decodeW > 0)
            {
                bmp.DecodePixelWidth = decodeW;
            }
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        private static void TrySaveCache(BitmapImage bmp, string cachePath)
        {
            try
            {
                if (File.Exists(cachePath))
                {
                    return;
                }
                var encoder = new JpegBitmapEncoder { QualityLevel = 88 };
                encoder.Frames.Add(BitmapFrame.Create(bmp));
                using (var fs = File.Create(cachePath))
                {
                    encoder.Save(fs);
                }
            }
            catch
            {
            }
        }

        private static string Hash(string s)
        {
            using (var sha = SHA1.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s));
                var sb = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++)
                {
                    sb.Append(bytes[i].ToString("x2"));
                }
                return sb.ToString();
            }
        }
    }
}
