using FastRawSelector.LOGIC;
using FastRawSelector.MODEL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FastRawSelector.CONTROL
{
    /// <summary>
    /// 그리드 뷰: 좌측 폴더 트리 + 우측 가상화 썸네일.
    /// - UI 칸: 보이는 인덱스만 GridItemControl 생성(풀 재사용)
    /// - 비트맵: 디스크 캐시 + ImageArray, 없으면 GridThumbLoader 로 RAW 디코드
    /// </summary>
    public partial class GridViewControl : UserControl
    {
        private readonly object dummyNode = new object();
        private readonly Dictionary<string, GridItemControl> _itemsByPath =
            new Dictionary<string, GridItemControl>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<int, GridItemControl> _realized =
            new Dictionary<int, GridItemControl>();
        private readonly Stack<GridItemControl> _pool = new Stack<GridItemControl>();
        private readonly List<ImageData> _dataList = new List<ImageData>();

        private string _selectedTreePath;
        private string _highlightPath;
        private GridItemControl _currentItem;
        private bool _suppressSizeSave;
        private int _itemSize = 160;
        private bool _viewportUpdateQueued;

        /// <summary>뷰포트 밖 여유(px).</summary>
        private const double ViewportMargin = 600;

        public string SelectedImagePath { get; set; }

        private double CellW { get { return _itemSize; } }
        private double CellH { get { return _itemSize + 18; } }

        public GridViewControl()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyTreeForeground();
            ApplySavedLayout();
            EnsureTreeRoot();
            SyncTreeToCurrentFolder();
            QueueViewportUpdate();
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.WidthChanged)
            {
                QueueViewportUpdate();
            }
        }

        public void ApplyTreeTheme()
        {
            ApplyTreeForeground();
            RefreshTreeItemForegrounds(foldersItem.Items);
        }

        /// <summary>언어 변경 시 라벨·상태줄 재적용.</summary>
        public void ApplyLanguage()
        {
            try
            {
                if (FolderLabelTb != null)
                {
                    FolderLabelTb.Text = Loc.Get("GridFolder");
                }
                if (OpenFolderBt != null)
                {
                    OpenFolderBt.Content = Loc.Get("GridOpenFolder");
                }
                if (SizeLabelTb != null)
                {
                    SizeLabelTb.Text = Loc.Get("GridSize");
                }
                if (RefreshGridBt != null)
                {
                    RefreshGridBt.Content = Loc.Get("GridRefresh");
                }
                RefreshStatusText();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>상태줄 (장 수 · 표시 · 품질 · 경로) 현재 언어로 갱신.</summary>
        private void RefreshStatusText()
        {
            if (StatusTb == null)
            {
                return;
            }
            int n = _dataList != null ? _dataList.Count : 0;
            if (n == 0)
            {
                StatusTb.Text = Loc.Get("GridNoOpenImage");
                return;
            }
            int realized = _realized != null ? _realized.Count : 0;
            StatusTb.Text = Loc.GetFormat(
                "GridStatus",
                n, realized, GridThumbCache.MaxDecodeWidth, LoadImage.NowDir ?? "");
        }

        /// <summary>테마 전환 후 트리·그리드 아이템 브러시 재적용.</summary>
        public void ApplyThemeChrome()
        {
            ApplyTreeTheme();
            foreach (var kv in _realized)
            {
                var item = kv.Value;
                if (item == null)
                {
                    continue;
                }
                item.RefreshThemeChrome();
                bool isCurrent = _currentItem == item;
                item.SetCurrent(isCurrent);
            }
        }

        private void ApplyTreeForeground()
        {
            var body = GetTreeForegroundBrush();
            foldersItem.Foreground = body;
            foldersItem.Resources[SystemColors.ControlTextBrushKey] = body;
            foldersItem.Resources[SystemColors.WindowTextBrushKey] = body;
            foldersItem.Resources[SystemColors.HighlightTextBrushKey] = body;
        }

        private Brush GetTreeForegroundBrush()
        {
            return TryFindResource("MaterialDesignBody") as Brush
                ?? new SolidColorBrush(Color.FromRgb(0xEE, 0xEE, 0xEE));
        }

        private void RefreshTreeItemForegrounds(ItemCollection items)
        {
            var body = GetTreeForegroundBrush();
            foreach (var obj in items)
            {
                var tvi = obj as TreeViewItem;
                if (tvi == null)
                {
                    continue;
                }
                tvi.Foreground = body;
                if (tvi.Items.Count > 0 && !(tvi.Items.Count == 1 && tvi.Items[0] == dummyNode))
                {
                    RefreshTreeItemForegrounds(tvi.Items);
                }
            }
        }

        private void ApplySavedLayout()
        {
            _suppressSizeSave = true;
            try
            {
                if (App.Setting != null)
                {
                    _itemSize = App.Setting.GridItemSize;
                    if (_itemSize < 100)
                    {
                        _itemSize = 100;
                    }
                    if (_itemSize > 1200)
                    {
                        _itemSize = 1200;
                    }
                    ItemSizeSlider.Value = _itemSize;
                    ItemSizeTb.Text = _itemSize.ToString();

                    double w = App.Setting.GridPaneWidth;
                    if (w < 120)
                    {
                        w = 120;
                    }
                    if (w > 480)
                    {
                        w = 480;
                    }
                    LeftCol.Width = new GridLength(w);
                }
                else
                {
                    ItemSizeSlider.Value = _itemSize;
                    ItemSizeTb.Text = _itemSize.ToString();
                }
            }
            finally
            {
                _suppressSizeSave = false;
            }
        }

        private void EnsureTreeRoot()
        {
            if (foldersItem.Items.Count > 0)
            {
                return;
            }
            try
            {
                foreach (string s in Directory.GetLogicalDrives())
                {
                    foldersItem.Items.Add(CreateFolderItem(s, s));
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private TreeViewItem CreateFolderItem(string header, string fullPath)
        {
            var item = new TreeViewItem
            {
                Header = header,
                Tag = fullPath,
                FontWeight = FontWeights.Normal,
                FontSize = 11,
                Padding = new Thickness(1),
                Margin = new Thickness(0),
                MinHeight = 18,
                Foreground = GetTreeForegroundBrush()
            };
            item.Items.Add(dummyNode);
            item.Expanded += folder_Expanded;
            return item;
        }

        private void folder_Expanded(object sender, RoutedEventArgs e)
        {
            PopulateChildren(sender as TreeViewItem);
        }

        private void PopulateChildren(TreeViewItem item)
        {
            if (item == null)
            {
                return;
            }
            if (item.Items.Count == 1 && item.Items[0] == dummyNode)
            {
                item.Items.Clear();
                try
                {
                    string path = item.Tag as string;
                    if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
                    {
                        return;
                    }
                    foreach (string s in Directory.GetDirectories(path))
                    {
                        string name = Path.GetFileName(s);
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }
                        item.Items.Add(CreateFolderItem(name, s));
                    }
                }
                catch (Exception)
                {
                }
            }
        }

        private void foldersItem_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var temp = foldersItem.SelectedItem as TreeViewItem;
            if (temp == null)
            {
                return;
            }
            _selectedTreePath = temp.Tag as string;
            SelectedImagePath = _selectedTreePath;
        }

        private void OpenFolderBt_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedTreePath) || !Directory.Exists(_selectedTreePath))
            {
                return;
            }
            try
            {
                var files = Directory.GetFiles(_selectedTreePath, "*", SearchOption.TopDirectoryOnly).ToList();
                files.Sort();
                foreach (var file in files)
                {
                    if (Common.IsRawFile(file)
                        || (App.Setting != null && App.Setting.AllowNotRawImage && Common.IsBitmapFile(file)))
                    {
                        LoadImage.SetArg(file);
                        return;
                    }
                }
                Log.Info("그리드: 선택한 폴더에 이미지 없음 " + _selectedTreePath);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void RefreshBt_Click(object sender, RoutedEventArgs e)
        {
            RefreshItems();
        }

        private void GridSplitter_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SavePaneWidth();
            QueueViewportUpdate();
        }

        private void SavePaneWidth()
        {
            try
            {
                if (App.Setting == null)
                {
                    return;
                }
                double w = LeftCol.ActualWidth;
                if (w < 120)
                {
                    w = 120;
                }
                if (Math.Abs(App.Setting.GridPaneWidth - w) < 0.5)
                {
                    return;
                }
                App.Setting.GridPaneWidth = w;
                App.Setting.Save(false);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void ItemSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (!IsLoaded || ItemSizeTb == null)
            {
                return;
            }
            int size = (int)Math.Round(e.NewValue);
            _itemSize = size;
            ItemSizeTb.Text = size.ToString();
            // 가상화: 전체 재배치
            QueueViewportUpdate();
        }

        private void ItemSizeSlider_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SaveItemSize();
            // 칸 크기 변경 → 필요 시 더 높은 해상도 재로드
            foreach (var item in _realized.Values)
            {
                item.ApplySize(_itemSize);
                item.EnsureThumbnail();
            }
            QueueViewportUpdate();
        }

        private void SaveItemSize()
        {
            if (_suppressSizeSave || App.Setting == null)
            {
                return;
            }
            try
            {
                int size = (int)Math.Round(ItemSizeSlider.Value);
                if (App.Setting.GridItemSize == size)
                {
                    return;
                }
                App.Setting.GridItemSize = size;
                App.Setting.Save(false);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private void MainScrollView_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.VerticalChange != 0 || e.ViewportHeightChange != 0
                || e.ViewportWidthChange != 0 || e.ExtentHeightChange != 0)
            {
                QueueViewportUpdate();
            }
        }

        private void QueueViewportUpdate()
        {
            if (_viewportUpdateQueued)
            {
                return;
            }
            _viewportUpdateQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _viewportUpdateQueued = false;
                RealizeViewport();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private int GetColumnCount()
        {
            double w = MainScrollView != null ? MainScrollView.ViewportWidth : 0;
            if (w <= 1)
            {
                w = Math.Max(200, ActualWidth - LeftCol.ActualWidth - 20);
            }
            return Math.Max(1, (int)(w / CellW));
        }

        /// <summary>
        /// 보이는 행 구간의 인덱스만 컨트롤 생성. 나머지는 풀로 반환.
        /// </summary>
        private void RealizeViewport()
        {
            if (ImageHost == null || MainScrollView == null)
            {
                return;
            }

            int n = _dataList.Count;
            int cols = GetColumnCount();
            int rows = n == 0 ? 0 : (n + cols - 1) / cols;

            double hostW = Math.Max(MainScrollView.ViewportWidth, cols * CellW);
            if (hostW < 1)
            {
                hostW = cols * CellW;
            }
            ImageHost.Width = hostW;
            ImageHost.Height = Math.Max(rows * CellH, MainScrollView.ViewportHeight);

            if (n == 0)
            {
                RecycleAll();
                return;
            }

            double offset = MainScrollView.VerticalOffset;
            double viewportH = MainScrollView.ViewportHeight;
            if (viewportH <= 0)
            {
                viewportH = Math.Max(200, ActualHeight - 100);
            }

            int firstRow = Math.Max(0, (int)Math.Floor((offset - ViewportMargin) / CellH));
            int lastRow = Math.Min(rows - 1, (int)Math.Floor((offset + viewportH + ViewportMargin) / CellH));
            int firstIdx = firstRow * cols;
            int lastIdx = Math.Min(n - 1, (lastRow + 1) * cols - 1);

            // 범위 밖 회수
            var remove = new List<int>();
            foreach (var key in _realized.Keys)
            {
                if (key < firstIdx || key > lastIdx)
                {
                    remove.Add(key);
                }
            }
            foreach (int idx in remove)
            {
                RecycleAt(idx);
            }

            // 범위 안 생성/배치
            int realized = 0;
            for (int i = firstIdx; i <= lastIdx; i++)
            {
                ImageData data = _dataList[i];
                GridItemControl item;
                if (!_realized.TryGetValue(i, out item))
                {
                    item = Rent();
                    item.ApplySize(_itemSize);
                    item.Bind(data);
                    ImageHost.Children.Add(item);
                    _realized[i] = item;
                    if (data != null && data.Path != null)
                    {
                        _itemsByPath[data.Path] = item;
                    }
                }
                else
                {
                    item.ApplySize(_itemSize);
                    // 풀 재사용·목록 갱신 시 Data 와 인덱스가 어긋나면 재바인딩
                    if (data != null && !object.ReferenceEquals(item.Data, data))
                    {
                        if (item.Data != null && item.Data.Path != null)
                        {
                            _itemsByPath.Remove(item.Data.Path);
                        }
                        item.Bind(data);
                        if (data.Path != null)
                        {
                            _itemsByPath[data.Path] = item;
                        }
                    }
                }

                int row = i / cols;
                int col = i % cols;
                Canvas.SetLeft(item, col * CellW);
                Canvas.SetTop(item, row * CellH);

                item.EnsureThumbnail();
                item.UpdateSelectionMark();

                bool isCurrent = _highlightPath != null
                    && data != null
                    && data.Path != null
                    && string.Equals(data.Path, _highlightPath, StringComparison.OrdinalIgnoreCase);
                item.SetCurrent(isCurrent);
                if (isCurrent)
                {
                    _currentItem = item;
                }

                realized++;
            }

            RefreshStatusText();

            Log.Debug(string.Format("grid virt: data={0}, realized={1}, range={2}-{3}, cols={4}",
                n, realized, firstIdx, lastIdx, cols));
        }

        private GridItemControl Rent()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            var item = new GridItemControl();
            item.ItemClick += OnItemClick;
            item.ItemDoubleClick += OnItemDoubleClick;
            return item;
        }

        private void RecycleAt(int index)
        {
            GridItemControl item;
            if (!_realized.TryGetValue(index, out item))
            {
                return;
            }
            if (item.Data != null && item.Data.Path != null)
            {
                _itemsByPath.Remove(item.Data.Path);
            }
            if (_currentItem == item)
            {
                _currentItem = null;
            }
            item.ClearThumbnail();
            item.SetCurrent(false);
            ImageHost.Children.Remove(item);
            _realized.Remove(index);
            _pool.Push(item);
        }

        private void RecycleAll()
        {
            var keys = _realized.Keys.ToList();
            foreach (int k in keys)
            {
                RecycleAt(k);
            }
            ImageHost.Children.Clear();
            _itemsByPath.Clear();
            _currentItem = null;
            ImageHost.Height = 0;
        }

        #region Tree sync (현재 폴더 펼침)

        public void SyncTreeToCurrentFolder()
        {
            try
            {
                EnsureTreeRoot();
                ApplyTreeForeground();

                string dir = LoadImage.NowDir;
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                {
                    return;
                }

                dir = Path.GetFullPath(dir);
                string root = Path.GetPathRoot(dir);
                if (string.IsNullOrEmpty(root))
                {
                    return;
                }

                TreeViewItem node = FindDriveNode(root);
                if (node == null)
                {
                    return;
                }

                PopulateChildren(node);
                node.IsExpanded = true;

                string fullNorm = NormalizePath(dir);
                string rootNorm = NormalizePath(root);

                if (string.Equals(fullNorm, rootNorm, StringComparison.OrdinalIgnoreCase))
                {
                    FinishSelectTreeNode(node);
                    return;
                }

                string relative = fullNorm.Length > rootNorm.Length
                    ? fullNorm.Substring(rootNorm.Length).TrimStart('\\', '/')
                    : "";
                string[] parts = relative.Split(new[] { '\\', '/' }, StringSplitOptions.RemoveEmptyEntries);

                string pathSoFar = root.EndsWith("\\") || root.EndsWith("/")
                    ? root
                    : root + Path.DirectorySeparatorChar;

                foreach (string part in parts)
                {
                    pathSoFar = Path.Combine(pathSoFar, part);
                    PopulateChildren(node);
                    node.IsExpanded = true;
                    node.UpdateLayout();

                    TreeViewItem child = FindChildByPath(node, pathSoFar);
                    if (child == null)
                    {
                        break;
                    }
                    node = child;
                    node.IsExpanded = true;
                }

                FinishSelectTreeNode(node);
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        private static string NormalizePath(string path)
        {
            try
            {
                return Path.GetFullPath(path).TrimEnd('\\', '/');
            }
            catch
            {
                return (path ?? "").TrimEnd('\\', '/');
            }
        }

        private void FinishSelectTreeNode(TreeViewItem node)
        {
            if (node == null)
            {
                return;
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                try
                {
                    ExpandAncestors(node);
                    node.IsSelected = true;
                    node.BringIntoView();
                    _selectedTreePath = node.Tag as string;
                    SelectedImagePath = _selectedTreePath;
                }
                catch (Exception ex)
                {
                    Log.Exception(ex);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private static void ExpandAncestors(TreeViewItem item)
        {
            ItemsControl parent = ItemsControl.ItemsControlFromItemContainer(item);
            while (parent is TreeViewItem tvi)
            {
                tvi.IsExpanded = true;
                parent = ItemsControl.ItemsControlFromItemContainer(tvi);
            }
        }

        private TreeViewItem FindDriveNode(string root)
        {
            string norm = NormalizePath(root);
            foreach (var obj in foldersItem.Items)
            {
                var item = obj as TreeViewItem;
                if (item == null)
                {
                    continue;
                }
                string tag = item.Tag as string ?? "";
                if (string.Equals(NormalizePath(tag), norm, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        private static TreeViewItem FindChildByPath(TreeViewItem parent, string fullPath)
        {
            string norm = NormalizePath(fullPath);
            string name = Path.GetFileName(norm);
            foreach (var obj in parent.Items)
            {
                var item = obj as TreeViewItem;
                if (item == null || item.Tag == null)
                {
                    continue;
                }
                string tag = NormalizePath(item.Tag.ToString());
                if (string.Equals(tag, norm, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
                if (!string.IsNullOrEmpty(name)
                    && item.Header != null
                    && string.Equals(item.Header.ToString(), name, StringComparison.OrdinalIgnoreCase))
                {
                    return item;
                }
            }
            return null;
        }

        #endregion

        /// <summary>
        /// 스크롤 위치 유지한 채 보이는 칸 썸네일만 다시 로드.
        /// AllLoad 완료 시 사용 — RefreshItems 는 현재 장으로 ScrollTo 해서 맨 위로 튀는 문제 있음.
        /// </summary>
        public void RefreshVisibleThumbnails()
        {
            try
            {
                foreach (var item in _realized.Values)
                {
                    if (item == null)
                    {
                        continue;
                    }
                    // ImageArray/디스크 캐시가 생겼을 수 있음
                    item.UpdateThumbnail();
                    item.UpdateSelectionMark();
                }
                RefreshStatusText();
            }
            catch (Exception ex)
            {
                Log.Exception(ex);
            }
        }

        /// <summary>데이터 목록 재구성 + 가상화 갱신.</summary>
        /// <param name="preserveScroll">true 이면 현재 스크롤 오프셋 유지 (로딩 완료 등).</param>
        public void RefreshItems(bool preserveScroll = false)
        {
            double savedOffset = 0;
            if (preserveScroll && MainScrollView != null)
            {
                savedOffset = MainScrollView.VerticalOffset;
            }

            RecycleAll();
            _dataList.Clear();
            _highlightPath = LoadImage.NowImage != null ? LoadImage.NowImage.Path : null;

            SyncTreeToCurrentFolder();

            if (LoadImage.ImageList == null || LoadImage.ImageList.Count == 0)
            {
                StatusTb.Text = Loc.Get("GridNoOpenImage");
                ImageHost.Height = 0;
                return;
            }

            if (App.Setting != null)
            {
                _itemSize = App.Setting.GridItemSize;
            }

            foreach (var kv in LoadImage.ImageList)
            {
                var data = kv.Value;
                if (data == null || data.IsNotImage)
                {
                    continue;
                }
                if (Common.IsOnlySelectedShow && Common.NowSelectorSetting != null)
                {
                    bool sel;
                    lock (Common.NowSelectorSetting.SyncRoot)
                    {
                        sel = Common.NowSelectorSetting.SelectedSet.Contains(data.FileName);
                    }
                    if (!sel)
                    {
                        continue;
                    }
                }
                _dataList.Add(data);
            }

            if (preserveScroll)
            {
                // 레이아웃 반영 후 오프셋 복원 (Extent 가 다시 잡힌 뒤)
                double off = savedOffset;
                QueueViewportUpdate();
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (MainScrollView != null)
                        {
                            MainScrollView.ScrollToVerticalOffset(off);
                        }
                        QueueViewportUpdate();
                    }
                    catch (Exception ex)
                    {
                        Log.Exception(ex);
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                // 뷰 전환 등: 현재 장 위치로 스크롤
                if (_highlightPath != null)
                {
                    ScrollToPath(_highlightPath, false);
                }

                QueueViewportUpdate();
                Dispatcher.BeginInvoke(new Action(QueueViewportUpdate),
                    System.Windows.Threading.DispatcherPriority.Loaded);
            }

            Log.Debug("그리드 데이터: " + _dataList.Count + " (virtualized), preserveScroll=" + preserveScroll);
        }

        public void UpdateThumbnail(ImageData data)
        {
            if (data == null || data.Path == null)
            {
                return;
            }
            GridItemControl item;
            if (_itemsByPath.TryGetValue(data.Path, out item))
            {
                item.EnsureThumbnail();
            }
        }

        public void Highlight(ImageData data)
        {
            if (_currentItem != null)
            {
                _currentItem.SetCurrent(false);
                _currentItem = null;
            }
            _highlightPath = data != null ? data.Path : null;
            if (data == null)
            {
                return;
            }

            ScrollToPath(data.Path, true);
            QueueViewportUpdate();
        }

        private void ScrollToPath(string path, bool animate)
        {
            if (string.IsNullOrEmpty(path) || _dataList.Count == 0)
            {
                return;
            }
            int idx = -1;
            for (int i = 0; i < _dataList.Count; i++)
            {
                if (string.Equals(_dataList[i].Path, path, StringComparison.OrdinalIgnoreCase))
                {
                    idx = i;
                    break;
                }
            }
            if (idx < 0 || MainScrollView == null)
            {
                return;
            }
            int cols = GetColumnCount();
            int row = idx / cols;
            double y = row * CellH;
            double target = Math.Max(0, y - MainScrollView.ViewportHeight * 0.25);
            MainScrollView.ScrollToVerticalOffset(target);
        }

        public void RefreshSelection()
        {
            foreach (var item in _realized.Values)
            {
                item.UpdateSelectionMark();
            }
        }

        private void OnItemClick(GridItemControl item)
        {
            if (item == null || item.Data == null)
            {
                return;
            }
            // Data 참조를 로컬에 고정 (가상화 재바인딩과 경합 방지)
            ImageData data = item.Data;
            NavigateTo(data);
        }

        private void OnItemDoubleClick(GridItemControl item)
        {
            if (item == null || item.Data == null)
            {
                return;
            }
            ImageData data = item.Data;
            NavigateTo(data);
            if (Common.Main != null)
            {
                Common.Main.ViewChange(ViewEnum.Single);
            }
        }

        /// <summary>
        /// 그리드 칸 선택. Prev/Next 와 같이 NowIndex 를 먼저 고정해야
        /// GetThum(IsSetImage) 이 Sleep 후 NowIndex 불일치로 표시를 포기하지 않는다.
        /// (ImageArray 없는 멀리 있는 장을 클릭하면 이전 장이 남는 문제)
        /// </summary>
        private void NavigateTo(ImageData data)
        {
            if (data == null)
            {
                return;
            }

            // 1) 표시 의도 인덱스를 즉시 고정 (GetThum 의 IsSetImage 판별 + NearLoad 중심)
            LoadImage.NowIndex = data.Index;
            LoadImage.NowImage = data;
            Highlight(data);

            if (data.IsNotImage)
            {
                return;
            }

            if (data.ImageArray != null && data.ImageArray.Length > 0)
            {
                LoadImage.SetImage(data);
                LoadImage.SetExif(data);
                if (App.Setting != null && App.Setting.IsExifVisible)
                {
                    LoadImage.EnsureExifAsync(data);
                }
            }
            else
            {
                // ImageArray 없음: 백그라운드 디코드 후 SetImage (NowIndex 이미 맞춤)
                LoadImage.GetThum(data, true);
            }

            LoadImage.NearLoad();
        }
    }
}
