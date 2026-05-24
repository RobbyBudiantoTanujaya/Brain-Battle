using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BrainBattle.Core.Models;
using BrainBattle.Shared.UI;

namespace BrainBattle.Games.Kings.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class KingsGridRenderer : MonoBehaviour
    {
        [SerializeField] private Sprite _dotSprite;
        [SerializeField] private Sprite _crownSprite;
        [SerializeField] private float  _padding       = DesignSystem.GridPadding;
        [SerializeField] private Color  _conflictColor = Color.red;
        [SerializeField] private float  _pulseSpeed    = 0.3f;

        /// <summary>Fired when the user taps (or presses) a cell. Args: (row, col).</summary>
        public event Action<int, int>             OnCellTapped;
        /// <summary>Fired while dragging across cells after the first. Args: (row, col, targetState).</summary>
        public event Action<int, int, CellState>  OnCellDragEntered;

        private GridData            _currentGrid;
        private RectTransform       _self;
        private RectTransform       _gridPanel;
        private CellView[,]         _cellViews;
        private float               _cellW;     // cell pixel width
        private float               _cellH;     // cell pixel height  (≤ _cellW × MaxCellAspect)
        private float               _cellSize;  // min(_cellW, _cellH) — used for icon sizing
        private HashSet<Vector2Int> _activeConflicts = new();
        private Coroutine           _conflictCoroutine;

        // Drag state — shared with CellEventHandler instances.
        private bool       _isDragging;
        private CellState  _dragTargetState;
        private Vector2Int _lastDragCell = new(-1, -1);

        private sealed class CellView
        {
            public Image         Background;
            public Image         Icon;
            public RectTransform IconRt;
            public Color         BaseColor;
        }

        // ── Cell interaction component ────────────────────────────────────────────

        private sealed class CellEventHandler
            : MonoBehaviour, IPointerDownHandler, IPointerEnterHandler, IPointerUpHandler
        {
            public int              Row;
            public int              Col;
            public KingsGridRenderer Renderer;

            public void OnPointerDown(PointerEventData eventData)  => Renderer.BeginCellInteraction(Row, Col);
            public void OnPointerEnter(PointerEventData eventData) => Renderer.ContinueDrag(Row, Col);
            public void OnPointerUp(PointerEventData eventData)    => Renderer.EndDrag();
        }

        private void Awake()
        {
            _self = GetComponent<RectTransform>();
            // Anchor/size is set entirely by KingsSceneBuilder; do not override here.
            // For point-anchor (legacy) layouts, ensure the container stays centred.
            if (_self.anchorMin == _self.anchorMax)
                _self.anchoredPosition = Vector2.zero;

            // Load sprites from Resources if not pre-assigned via Inspector / SceneBuilder.
            // dot.png is a single sprite (dot_0).
            // crown.png is a multi-sprite sheet: crown_0=small circle, crown_1=crown icon, crown_2=thin bar.
            // Use LoadAll and pick crown_1 (the actual crown shape).
            if (_dotSprite == null)
            {
                var dots = Resources.LoadAll<Sprite>("Sprites/dot");
                _dotSprite = dots.Length > 0 ? dots[0] : null;
            }
            if (_crownSprite == null)
            {
                var crowns = Resources.LoadAll<Sprite>("Sprites/crown");
                // crown_1 is the full crown icon (347x224 px region)
                foreach (var s in crowns) { if (s.name == "crown_1") { _crownSprite = s; break; } }
                if (_crownSprite == null && crowns.Length > 0) _crownSprite = crowns[crowns.Length - 1];
            }
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void RenderGrid(GridData grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            Debug.Log($"[KingsGridRenderer] RenderGrid called — gridSize={grid.Size}, regions={grid.Regions.Count}");

            ClearConflicts();
            DestroyGridPanel();

            _currentGrid = grid;
            _cellViews   = new CellView[grid.Size, grid.Size];

            Canvas.ForceUpdateCanvases();

            // Read from GridContainer's own rect — set by KingsSceneBuilder anchor band.
            Rect available = _self.rect;
            Debug.Log($"[KingsGridRenderer] _self.rect={available.size}");

            // Symmetric 16 px padding on all sides.
            float availableW = available.width  - _padding * 2f;
            float availableH = available.height - _padding * 2f;

            if (availableW <= 0f || availableH <= 0f)
            {
                Debug.LogWarning("[KingsGridRenderer] _self.rect is zero — Canvas not yet laid out. Falling back to 320px.");
                availableW = availableH = 320f - _padding * 2f;
            }

            // Cells are always square — use the smaller axis so the grid fits on screen.
            _cellSize = Mathf.Min(availableW, availableH) / grid.Size;
            _cellW    = _cellSize;
            _cellH    = _cellSize;

            Debug.Log($"[KingsGridRenderer] cellW={_cellW:F1}  cellH={_cellH:F1}  cellSize={_cellSize:F1}");

            _gridPanel                  = CreatePanel("GridPanel", _self);
            _gridPanel.anchorMin        = new Vector2(0.5f, 0.5f);
            _gridPanel.anchorMax        = new Vector2(0.5f, 0.5f);
            _gridPanel.pivot            = new Vector2(0.5f, 0.5f);
            _gridPanel.anchoredPosition = Vector2.zero;

            BuildCells(grid);

            float gridTotalW = _cellW * grid.Size;
            float gridTotalH = _cellH * grid.Size;
            _gridPanel.sizeDelta = new Vector2(gridTotalW, gridTotalH);
            Debug.Log($"[KingsGridRenderer] complete — grid={gridTotalW:F0}×{gridTotalH:F0}  container={available.size}");
        }

        public void UpdateCell(int row, int col, CellState state)
        {
            if (_cellViews == null || _currentGrid == null) return;
            if ((uint)row >= (uint)_currentGrid.Size || (uint)col >= (uint)_currentGrid.Size) return;
            ApplyCellState(_cellViews[row, col], state);
        }

        /// <summary>
        /// Begins flashing the given cells red. Positions use the project convention: x = col, y = row.
        /// Passing null or an empty list stops any active flash.
        /// </summary>
        public void HighlightConflicts(List<Vector2Int> conflicts)
        {
            _activeConflicts.Clear();

            if (conflicts == null || conflicts.Count == 0)
            {
                if (_conflictCoroutine != null)
                {
                    StopCoroutine(_conflictCoroutine);
                    _conflictCoroutine = null;
                }
                return;
            }

            foreach (var pos in conflicts)
                _activeConflicts.Add(pos);

            if (_conflictCoroutine != null) StopCoroutine(_conflictCoroutine);
            _conflictCoroutine = StartCoroutine(PulseConflicts());
        }

        public void ClearConflicts()
        {
            if (_conflictCoroutine != null)
            {
                StopCoroutine(_conflictCoroutine);
                _conflictCoroutine = null;
            }

            RestoreConflictColors();
            _activeConflicts.Clear();
        }

        // ── Drag interaction ──────────────────────────────────────────────────────

        internal void BeginCellInteraction(int row, int col)
        {
            _isDragging   = true;
            _lastDragCell = new Vector2Int(col, row);

            // Fire tap first (game manager cycles the state synchronously).
            OnCellTapped?.Invoke(row, col);

            // Read back the state the game manager applied as the drag target.
            if (_currentGrid != null)
                _dragTargetState = _currentGrid.GetCell(row, col).State;
        }

        internal void ContinueDrag(int row, int col)
        {
            if (!_isDragging) return;
            var pos = new Vector2Int(col, row);
            if (pos == _lastDragCell) return;
            _lastDragCell = pos;
            OnCellDragEntered?.Invoke(row, col, _dragTargetState);
        }

        internal void EndDrag() => _isDragging = false;

        // ── Grid building ─────────────────────────────────────────────────────────

        private void BuildCells(GridData grid)
        {
            var regionMap = new Dictionary<int, RegionData>(grid.Regions.Count);
            foreach (var region in grid.Regions)
                regionMap[region.RegionId] = region;

            int   size      = grid.Size;
            float bt        = DesignSystem.BorderRegionThickness;
            float thin      = DesignSystem.BorderCellThickness;
            Color darkColor = DesignSystem.BorderRegion;
            Color thinColor = DesignSystem.BorderCell;

            // ── Pass 1: cells ─────────────────────────────────────────────────────
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    CellData cell = grid.GetCell(r, c);
                    int      rid  = cell.RegionId;

                    regionMap.TryGetValue(rid, out RegionData region);
                    Color regionColor = (region != null && region.RegionColor != Color.white)
                        ? region.RegionColor
                        : Color.HSVToRGB((rid * 0.13f) % 1f, 0.45f, 0.85f);

                    var cellGo = new GameObject($"Cell_{r}_{c}", typeof(RectTransform));
                    var rt     = cellGo.GetComponent<RectTransform>();
                    rt.SetParent(_gridPanel, false);
                    rt.anchorMin        = new Vector2(0f, 1f);
                    rt.anchorMax        = new Vector2(0f, 1f);
                    rt.pivot            = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(c * _cellW, -r * _cellH);
                    rt.sizeDelta        = new Vector2(_cellW, _cellH);

                    var bgGo  = new GameObject("Bg", typeof(RectTransform), typeof(Image));
                    var bgRt  = bgGo.GetComponent<RectTransform>();
                    bgRt.SetParent(rt, false);
                    bgRt.anchorMin = Vector2.zero;
                    bgRt.anchorMax = Vector2.one;
                    bgRt.offsetMin = Vector2.zero;
                    bgRt.offsetMax = Vector2.zero;
                    var bgImg           = bgGo.GetComponent<Image>();
                    bgImg.color         = regionColor;
                    bgImg.raycastTarget = true;

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    var iconRt = iconGo.GetComponent<RectTransform>();
                    iconRt.SetParent(rt, false);
                    iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
                    iconRt.pivot            = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = Vector2.zero;
                    float iconSize          = _cellSize * DesignSystem.DotSizeRatio;
                    iconRt.sizeDelta        = new Vector2(iconSize, iconSize);

                    var icon            = iconGo.GetComponent<Image>();
                    icon.preserveAspect = true;
                    icon.raycastTarget  = false;

                    _cellViews[r, c] = new CellView
                        { Background = bgImg, Icon = icon, IconRt = iconRt, BaseColor = regionColor };
                    ApplyCellState(_cellViews[r, c], cell.State);

                    var handler      = cellGo.AddComponent<CellEventHandler>();
                    handler.Row      = r;
                    handler.Col      = c;
                    handler.Renderer = this;
                }
            }

            // ── Pass 2: vertical internal borders ────────────────────────────────
            for (int r = 0; r < size; r++)
                for (int c = 0; c < size - 1; c++)
                {
                    bool diff = grid.GetCell(r, c).RegionId != grid.GetCell(r, c + 1).RegionId;
                    CreateBorderLine(_gridPanel,
                        new Vector2((c + 1) * _cellW,       -(r + 0.5f) * _cellH),
                        new Vector2(diff ? bt : thin, _cellH),
                        diff ? darkColor : thinColor);
                }

            // ── Pass 3: horizontal internal borders ──────────────────────────────
            for (int r = 0; r < size - 1; r++)
                for (int c = 0; c < size; c++)
                {
                    bool diff = grid.GetCell(r, c).RegionId != grid.GetCell(r + 1, c).RegionId;
                    CreateBorderLine(_gridPanel,
                        new Vector2((c + 0.5f) * _cellW,    -(r + 1) * _cellH),
                        new Vector2(_cellW, diff ? bt : thin),
                        diff ? darkColor : thinColor);
                }

            // ── Pass 4: outer border frame ────────────────────────────────────────
            float gsW = _cellW * size;
            float gsH = _cellH * size;
            CreateBorderLine(_gridPanel, new Vector2(gsW * 0.5f,       bt * 0.5f),       new Vector2(gsW + bt, bt),       darkColor);
            CreateBorderLine(_gridPanel, new Vector2(gsW * 0.5f,      -gsH - bt * 0.5f), new Vector2(gsW + bt, bt),       darkColor);
            CreateBorderLine(_gridPanel, new Vector2(-bt * 0.5f,      -gsH * 0.5f),      new Vector2(bt, gsH + bt),       darkColor);
            CreateBorderLine(_gridPanel, new Vector2(gsW + bt * 0.5f, -gsH * 0.5f),      new Vector2(bt, gsH + bt),       darkColor);
        }

        /// <summary>
        /// Creates a single border line Image under <paramref name="parent"/>.
        /// Uses top-left anchor + center pivot, same coordinate space as the cell grid.
        /// </summary>
        private static void CreateBorderLine(RectTransform parent, Vector2 center, Vector2 size, Color color)
        {
            var go  = new GameObject("Border", typeof(RectTransform), typeof(Image));
            var rt  = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin        = new Vector2(0f, 1f);  // top-left anchor
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = center;
            rt.sizeDelta        = size;
            var img           = go.GetComponent<Image>();
            img.color         = color;
            img.raycastTarget = false;
        }

        // ── Cell state ────────────────────────────────────────────────────────────

        private void ApplyCellState(CellView view, CellState state)
        {
            switch (state)
            {
                case CellState.Empty:
                    view.Icon.enabled = false;
                    break;
                case CellState.Dot:
                    view.Icon.sprite  = _dotSprite;
                    view.Icon.enabled = _dotSprite != null;
                    if (view.IconRt != null)
                    {
                        float s = _cellSize * DesignSystem.DotSizeRatio;
                        view.IconRt.sizeDelta = new Vector2(s, s);
                    }
                    break;
                case CellState.Crown:
                    view.Icon.sprite  = _crownSprite;
                    view.Icon.enabled = _crownSprite != null;
                    if (view.IconRt != null)
                    {
                        float s = _cellSize * DesignSystem.CrownSizeRatio;
                        view.IconRt.sizeDelta = new Vector2(s, s);
                    }
                    break;
            }
        }

        // ── Conflict pulse ────────────────────────────────────────────────────────

        private IEnumerator PulseConflicts()
        {
            while (_activeConflicts.Count > 0)
            {
                // Sin gives a smooth 0→1→0 pulse; _pulseSpeed is the half-period in seconds.
                float t = Mathf.Abs(Mathf.Sin(Time.time * Mathf.PI / _pulseSpeed));
                ApplyConflictColors(t);
                yield return null;
            }

            RestoreConflictColors();
            _conflictCoroutine = null;
        }

        private void ApplyConflictColors(float t)
        {
            if (_cellViews == null) return;
            foreach (var pos in _activeConflicts)
            {
                if ((uint)pos.y >= (uint)_currentGrid.Size || (uint)pos.x >= (uint)_currentGrid.Size) continue;
                var view = _cellViews[pos.y, pos.x];
                view.Background.color = Color.Lerp(view.BaseColor, _conflictColor, t);
            }
        }

        private void RestoreConflictColors()
        {
            if (_cellViews == null) return;
            foreach (var pos in _activeConflicts)
            {
                if ((uint)pos.y >= (uint)_currentGrid.Size || (uint)pos.x >= (uint)_currentGrid.Size) continue;
                var view = _cellViews[pos.y, pos.x];
                view.Background.color = view.BaseColor;
            }
        }

        // ── Factories & cleanup ───────────────────────────────────────────────────

        private static RectTransform CreatePanel(string name, RectTransform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private void DestroyGridPanel()
        {
            _isDragging = false;

            if (_gridPanel != null)
            {
                Destroy(_gridPanel.gameObject);
                _gridPanel = null;
            }
            _cellViews = null;
        }
    }
}
