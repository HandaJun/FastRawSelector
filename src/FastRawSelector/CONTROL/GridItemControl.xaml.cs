using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FastRawSelector.CONTROL
{
    /// <summary>
    /// 그리드 칸. UI 크기(슬라이더)와 디코드 폭을 연동하되 MaxDecodeWidth 상한.
    /// 칸을 키우면 필요 시 더 높은 해상도로 재로드.
    /// </summary>
    public partial class GridItemControl : UserControl
    {
        public ImageData Data { get; private set; }

        private int _size = 160;
        private int _decodedWidth;
        private bool _thumbReady;
        /// <summary>Bind 세대 — 비동기 디코드 완료 시 다른 장에 붙지 않게.</summary>
        private int _bindGen;

        public event Action<GridItemControl> ItemClick;
        public event Action<GridItemControl> ItemDoubleClick;

        public GridItemControl()
        {
            InitializeComponent();
        }

        public bool HasThumbnail
        {
            get { return _thumbReady && ThumbImg.Source != null; }
        }

        /// <summary>레이아웃 한 변(px). 디코드 폭은 GetDecodeWidth()로 결정.</summary>
        public void ApplySize(int size)
        {
            if (size < 80)
            {
                size = 80;
            }
            if (size > 1200)
            {
                size = 1200;
            }
            int oldNeed = GridThumbCache.GetDecodeWidth(_size);
            _size = size;
            Width = size;
            Height = size + 18;
            double mark = Math.Max(12, Math.Min(28, size * 0.1));
            SelectedMark.Width = mark;
            SelectedMark.Height = mark;
            SelectedMark.CornerRadius = new CornerRadius(mark / 2);
            FileNameTb.FontSize = size < 120 ? 9 : (size > 400 ? 13 : (size > 220 ? 12 : 10));
            PlaceholderTb.FontSize = Math.Max(16, Math.Min(48, size * 0.12));

            // 칸이 커져 더 높은 디코드가 필요하면 다음 Ensure 때 재로드
            int newNeed = GridThumbCache.GetDecodeWidth(_size);
            if (_thumbReady && _decodedWidth < newNeed)
            {
                ClearThumbnail();
            }
        }

        public void Bind(ImageData data)
        {
            Data = data;
            _bindGen++;
            _thumbReady = false;
            _decodedWidth = 0;
            if (data == null)
            {
                return;
            }
            FileNameTb.Text = data.FileName ?? "";
            ClearThumbnail();
            UpdateSelectionMark();
        }

        public void ClearThumbnail()
        {
            ThumbImg.Source = null;
            _thumbReady = false;
            _decodedWidth = 0;
            PlaceholderTb.Visibility = Visibility.Visible;
        }

        public void EnsureThumbnail()
        {
            int need = GridThumbCache.GetDecodeWidth(_size);
            if (_thumbReady && ThumbImg.Source != null && _decodedWidth >= need)
            {
                return;
            }
            LoadThumbnail();
        }

        public void UpdateThumbnail()
        {
            _thumbReady = false;
            _decodedWidth = 0;
            LoadThumbnail();
        }

        private void LoadThumbnail()
        {
            if (Data == null)
            {
                ClearThumbnail();
                return;
            }
            try
            {
                int decodeW = GridThumbCache.GetDecodeWidth(_size);
                BitmapImage bmp = GridThumbCache.LoadThumb(Data.Path, Data.ImageArray, decodeW);
                if (bmp != null)
                {
                    ApplyThumb(bmp, decodeW);
                    return;
                }

                // 캐시 미스: 경량 그리드 디코드 (1616 싱글 경로 사용 안 함)
                ClearThumbnail();
                if (Data.IsNotImage || (!Data.IsRaw && !Data.IsBitmap))
                {
                    return;
                }

                string path = Data.Path;
                int gen = _bindGen;
                int needW = decodeW;
                GridThumbLoader.Request(Data, needW, (readyPath, readyBmp) =>
                {
                    if (gen != _bindGen || Data == null)
                    {
                        return;
                    }
                    if (!string.Equals(Data.Path, readyPath, StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                    if (readyBmp == null)
                    {
                        return;
                    }
                    // 이미 더 좋은 해상도가 있으면 유지
                    if (_thumbReady && _decodedWidth >= needW && ThumbImg.Source != null)
                    {
                        return;
                    }
                    ApplyThumb(readyBmp, needW);
                });
            }
            catch (Exception ex)
            {
                Log.ExceptionWithMsg(Data != null ? Data.Path : null, ex);
                ClearThumbnail();
            }
        }

        private void ApplyThumb(BitmapImage bmp, int decodeW)
        {
            if (bmp == null)
            {
                return;
            }
            ThumbImg.Source = bmp;
            _thumbReady = true;
            _decodedWidth = decodeW;
            PlaceholderTb.Visibility = Visibility.Collapsed;
        }

        public void UpdateSelectionMark()
        {
            bool selected = false;
            if (Data != null && Common.NowSelectorSetting != null)
            {
                lock (Common.NowSelectorSetting.SyncRoot)
                {
                    selected = Common.NowSelectorSetting.SelectedSet.Contains(Data.FileName);
                }
            }
            SelectedMark.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            // 선택 시 칸 배경을 아주 살짝 물들임
            RootBd.SetResourceReference(Border.BackgroundProperty,
                selected ? "AppSelectedItemBgBrush" : "AppGridItemBgBrush");
        }

        private bool _currentVisual;

        public void SetCurrent(bool isCurrent)
        {
            _currentVisual = isCurrent;
            // DynamicResource 유지 → 테마 전환 시 테두리 색 자동 갱신
            if (isCurrent)
            {
                RootBd.SetResourceReference(Border.BorderBrushProperty, "AppAccentBrush");
                RootBd.BorderThickness = new Thickness(3);
            }
            else
            {
                RootBd.SetResourceReference(Border.BorderBrushProperty, "AppGridItemBorderBrush");
                RootBd.BorderThickness = new Thickness(2);
            }
        }

        /// <summary>테마 전환 후 배경·선택 마크 재연결.</summary>
        public void RefreshThemeChrome()
        {
            UpdateSelectionMark();
            SelectedMark.SetResourceReference(Border.BackgroundProperty, "AppAccentBrush");
            FileNameTb.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
            PlaceholderTb.SetResourceReference(TextBlock.ForegroundProperty, "MaterialDesignBody");
            SetCurrent(_currentVisual);
        }

        private void UserControl_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 클릭 시점 Data 스냅샷 — 이벤트 처리 중 재바인딩 되어도 의도한 장 유지
            var data = Data;
            if (data == null)
            {
                return;
            }
            ItemClick?.Invoke(this);
            e.Handled = true;
        }

        private void UserControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (Data == null)
            {
                return;
            }
            ItemDoubleClick?.Invoke(this);
            e.Handled = true;
        }
    }
}
