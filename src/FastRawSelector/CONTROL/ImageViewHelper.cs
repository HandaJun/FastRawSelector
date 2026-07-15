using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace FastRawSelector.CONTROL
{
    /// <summary>
    /// SingleView / FullView 공통: EXIF 텍스트, 카운트, ToolTip, 테마 크롬 (Part 3).
    /// </summary>
    public static class ImageViewHelper
    {
        /// <summary>설정 단축키·언어에 맞춰 네비 컨트롤 ToolTip 갱신.</summary>
        public static void RefreshHotkeyTooltips(
            CheckBox selectCb,
            Button exifBt,
            Button prevBt,
            Button nextBt)
        {
            string sel = "B";
            string exif = "I";
            if (App.Setting != null)
            {
                sel = App.Setting.KeySelect ?? "B";
                exif = App.Setting.KeyExif ?? "I";
            }
            if (selectCb != null)
            {
                selectCb.ToolTip = Loc.GetFormat("TipSelect", sel);
            }
            if (exifBt != null)
            {
                exifBt.ToolTip = Loc.GetFormat("TipExif", exif);
            }
            if (prevBt != null)
            {
                prevBt.ToolTip = Loc.Get("TipPrev");
            }
            if (nextBt != null)
            {
                nextBt.ToolTip = Loc.Get("TipNext");
            }
        }

        /// <summary>테마 전환 후 아이콘/선택 오버레이/하단 라벨 브러시 재연결.</summary>
        public static void ApplyThemeChrome(
            Button prevBt,
            FrameworkElement prevIcon,
            Button nextBt,
            FrameworkElement nextIcon,
            Button exifBt,
            FrameworkElement exifIcon,
            Border selectedBd,
            TextBlock countTb,
            TextBlock fileNameTb)
        {
            BindIcon(prevBt, prevIcon, "AppIconBrush");
            BindIcon(nextBt, nextIcon, "AppIconBrush");
            BindIcon(exifBt, exifIcon, "AppExifIconBrush");
            if (selectedBd != null)
            {
                selectedBd.SetResourceReference(Border.BorderBrushProperty, "AppAccentBrush");
                selectedBd.SetResourceReference(Border.BackgroundProperty, "AppSelectedOverlayBrush");
            }
            if (countTb != null)
            {
                countTb.SetResourceReference(TextBlock.ForegroundProperty, "AppBottomLabelBrush");
            }
            if (fileNameTb != null)
            {
                fileNameTb.SetResourceReference(TextBlock.ForegroundProperty, "AppBottomLabelBrush");
            }
        }

        public static void BindIcon(Button button, FrameworkElement icon, string brushKey)
        {
            if (button != null)
            {
                button.SetResourceReference(Control.ForegroundProperty, brushKey);
            }
            if (icon != null)
            {
                icon.SetResourceReference(Control.ForegroundProperty, brushKey);
            }
        }

        /// <summary>RAW EXIF 중 표시용 필드만 모아 문자열로.</summary>
        public static string BuildExifText(ImageData data)
        {
            if (data == null || !App.Setting.IsExifVisible)
            {
                return string.Empty;
            }
            if (!data.IsRaw || data.Exif == null)
            {
                return string.Empty;
            }

            var exifText = new StringBuilder();
            foreach (var item in data.Exif)
            {
                if (string.IsNullOrEmpty(item.Value.Item2))
                {
                    continue;
                }
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
                        exifText.Append(item.Value.Item1 + " : " + item.Value.Item2 + "\n");
                        break;
                }
            }
            return exifText.ToString();
        }

        /// <summary>"현재 / 전체 (선택 N)" 카운트 문자열.</summary>
        public static string BuildCountText(int index, int selectedCount = 0)
        {
            string count = (index + 1) + " / " + (LoadImage.LastIndex + 1);
            if (selectedCount != 0)
            {
                count += Loc.GetFormat("SelectedCount", selectedCount);
            }
            return count;
        }

        /// <summary>선택 오버레이·체크박스 동기화.</summary>
        public static void ApplySelectedState(Border selectedBd, CheckBox selectCb, bool selected)
        {
            if (selectedBd != null)
            {
                selectedBd.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            }
            if (selectCb != null)
            {
                selectCb.IsChecked = selected;
            }
        }
    }
}
