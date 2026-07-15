using FastRawSelector.LOGIC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastRawSelector.MANAGER
{
    /// <summary>
    /// RAW 파일 썸네일 디코드 파이프라인.
    /// MetaProvider(exiv2)로 EXIF·임베디드 썸네일·사이즈·방향을 읽고,
    /// 장변 longEdge 로 스케일·회전한 뒤 BitmapSource 또는 JPEG byte[] 를 반환한다.
    /// 실패 시 (null, null) 과 Log.ExceptionWithMsg.
    /// </summary>
    public class RawManager
    {
        /// <summary>UI용 BitmapSource 썸네일 + EXIF 사전.</summary>
        public static (BitmapSource, SortedDictionary<string, (string, string)>) GetThumbnail(string path, double longEdge)
        {
            // 배열 경로와 동일 파이프라인 사용
            var arr = GetThumbnailArray(path, longEdge, loadExif: true);
            if (arr.Item1 == null)
            {
                return (null, null);
            }
            try
            {
                var img = new BitmapImage();
                using (var ms = new MemoryStream(arr.Item1))
                {
                    img.BeginInit();
                    img.StreamSource = ms;
                    img.CacheOption = BitmapCacheOption.OnLoad;
                    img.EndInit();
                }
                img.Freeze();
                return (img, arr.Item2);
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(path, ex);
                return (null, null);
            }
        }

        /// <summary>
        /// 캐시·내보내기용 JPEG byte[] 썸네일 + (선택) EXIF 사전.
        /// loadExif=false 여도 Orientation·원본 사이즈로 회전 보정은 항상 수행.
        /// </summary>
        public static (byte[], SortedDictionary<string, (string, string)>) GetThumbnailArray(
            string path, double longEdge, bool loadExif = true)
        {
            MetaProvider Meta = new MetaProvider(path);
            var orientation = Meta.GetOrientation();
            // 회전 판별용 원본 크기: EXIF 사전과 무관하게 센서/이미지 크기 (libraw)
            var sensorSize = Meta.GetSize(allowExifLookup: false);

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
                    // 임베디드 JPEG 의 EXIF Orientation 자동 적용 방지 (우리가 RAW Orientation 으로 통일)
                    img.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
                    img.EndInit();

                    var transformed = RotateAndScaleThumbnail(img, orientation, sensorSize, longEdge);
                    transformed.Freeze();

                    byte[] data;
                    JpegBitmapEncoder encoder = new JpegBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(transformed));
                    using (MemoryStream ms = new MemoryStream())
                    {
                        encoder.Save(ms);
                        data = ms.ToArray();
                    }

                    SortedDictionary<string, (string, string)> exif = null;
                    if (loadExif)
                    {
                        exif = Meta.GetExif();
                    }
                    return (data, exif);
                }
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(path, ex);
                return (null, null);
            }
        }

        /// <summary>EXIF 사전만 로드 (썸네일 이미 캐시된 뒤 지연 로드용).</summary>
        public static SortedDictionary<string, (string, string)> GetExifOnly(string path)
        {
            try
            {
                return new MetaProvider(path).GetExif();
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(path, ex);
                return null;
            }
        }

        /// <summary>
        /// 임베디드 썸네일에 EXIF Orientation 적용 후 longEdge 로 스케일.
        /// 일부 카메라(예: RX100)는 썸네일 픽셀이 이미 회전되어 있음 → 원본 종횡비와 비교해 이중 회전 방지.
        /// </summary>
        private static TransformedBitmap RotateAndScaleThumbnail(
            BitmapImage image,
            Orientation orientation,
            System.Windows.Size sensorSize,
            double longEdge)
        {
            bool axesSwapTag = orientation == Orientation.LeftTop
                || orientation == Orientation.RightTop
                || orientation == Orientation.RightBottom
                || orientation == Orientation.LeftBottom;

            bool sensorLandscape = sensorSize.Width >= sensorSize.Height;
            bool thumbLandscape = image.PixelWidth >= image.PixelHeight;

            // 90/270 계열: 센서와 썸네일 종횡비가 같으면 미회전 썸네일 → Orientation 적용
            // 종횡비가 다르면 이미 회전된 썸네일 → 적용 스킵
            bool applyOrientation;
            if (orientation == Orientation.Undefined || orientation == Orientation.TopLeft)
            {
                applyOrientation = false;
            }
            else if (axesSwapTag)
            {
                applyOrientation = (sensorLandscape == thumbLandscape);
            }
            else
            {
                // 2,3,4: 미러/180 — 종횡비로 판별 불가, EXIF 대로 적용
                applyOrientation = true;
            }

            var transforms = new TransformGroup();
            bool swapAfterRotate = false;

            if (applyOrientation)
            {
                switch (orientation)
                {
                    case Orientation.TopRight: // 2
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        break;
                    case Orientation.BottomRight: // 3
                        transforms.Children.Add(new RotateTransform(180));
                        break;
                    case Orientation.BottomLeft: // 4
                        transforms.Children.Add(new ScaleTransform(1, -1, 0, 0));
                        break;
                    case Orientation.LeftTop: // 5
                        transforms.Children.Add(new RotateTransform(90));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swapAfterRotate = true;
                        break;
                    case Orientation.RightTop: // 6 — 가장 흔한 세로 촬영
                        transforms.Children.Add(new RotateTransform(90));
                        swapAfterRotate = true;
                        break;
                    case Orientation.RightBottom: // 7
                        transforms.Children.Add(new RotateTransform(270));
                        transforms.Children.Add(new ScaleTransform(-1, 1, 0, 0));
                        swapAfterRotate = true;
                        break;
                    case Orientation.LeftBottom: // 8
                        transforms.Children.Add(new RotateTransform(270));
                        swapAfterRotate = true;
                        break;
                }
            }

            // 표시 목표 크기: 센서 크기 → Orientation 반영 후 longEdge
            double tw = sensorSize.Width;
            double th = sensorSize.Height;
            // 최종 화면 방향: 태그가 축 교환이면 표시 크기도 교환
            // (썸네일이 이미 회전된 경우에도 표시는 올바른 초상/가로)
            if (axesSwapTag)
            {
                double tmp = tw;
                tw = th;
                th = tmp;
            }

            if (tw >= th && tw > longEdge)
            {
                th = th * longEdge / tw;
                tw = longEdge;
            }
            else if (th > tw && th > longEdge)
            {
                tw = tw * longEdge / th;
                th = longEdge;
            }
            if (tw < 1)
            {
                tw = 1;
            }
            if (th < 1)
            {
                th = 1;
            }

            // 스케일: 회전 적용 후 유효 픽셀 크기 기준
            double srcW = (applyOrientation && swapAfterRotate) ? image.PixelHeight : image.PixelWidth;
            double srcH = (applyOrientation && swapAfterRotate) ? image.PixelWidth : image.PixelHeight;
            // 이미 회전된 썸네일(스킵)인데 axesSwap 표시 크기인 경우 썸네일 픽셀 그대로
            if (!applyOrientation)
            {
                srcW = image.PixelWidth;
                srcH = image.PixelHeight;
            }

            transforms.Children.Add(new ScaleTransform(tw / srcW, th / srcH));

            return new TransformedBitmap(image, transforms);
        }

        /// <summary>RAW 썸네일을 JPEG 파일로 저장. 이미 outputPath 가 있으면 덮어쓰지 않고 EXIF만 반환.</summary>
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
