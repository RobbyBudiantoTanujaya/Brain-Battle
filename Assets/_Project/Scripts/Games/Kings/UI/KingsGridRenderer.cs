using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using BrainBattle.Core.Models;

namespace BrainBattle.Games.Kings.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class KingsGridRenderer : MonoBehaviour
    {
        private const float IconScale = 0.55f;

        [SerializeField] private Sprite _dotSprite;
        [SerializeField] private Sprite _crownSprite;
        [SerializeField] private float  _padding          = 16f;
        [SerializeField] private Color  _thinBorderColor  = new Color(0f, 0f, 0f, 0.25f);
        [SerializeField] private Color  _thickBorderColor = new Color(0f, 0f, 0f, 0.80f);
        [SerializeField] private float  _thinBorderWidth  = 1.5f;
        [SerializeField] private float  _thickBorderWidth = 4f;
        [SerializeField] private Color  _conflictColor    = Color.red;
        [SerializeField] private float  _pulseSpeed       = 0.3f;

        /// <summary>Fired when the user taps (or presses) a cell. Args: (row, col).</summary>
        public event Action<int, int>             OnCellTapped;
        /// <summary>Fired while dragging across cells after the first. Args: (row, col, targetState).</summary>
        public event Action<int, int, CellState>  OnCellDragEntered;

        private GridData            _currentGrid;
        private RectTransform       _self;
        private RectTransform       _gridPanel;
        private CellView[,]         _cellViews;
        private float               _cellSize;
        private HashSet<Vector2Int> _activeConflicts = new();
        private Coroutine           _conflictCoroutine;

        // Drag state — shared with CellEventHandler instances.
        private bool       _isDragging;
        private CellState  _dragTargetState;
        private Vector2Int _lastDragCell = new(-1, -1);

        private sealed class CellView
        {
            public Image Background;
            public Image Icon;
            public Color BaseColor;
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

            // Read from GridContainer's own rect. Works for both layouts:
            //   • Stretch anchor (new): rect = canvas area above the HUD.
            //   • Point anchor  (legacy scene): rect = SceneBuilder-assigned sizeDelta.
            // We never mutate _self.sizeDelta here, so there is no feedback loop.
            Rect available = _self.rect;
            Debug.Log($"[KingsGridRenderer] _self.rect={available.size}");
            // Side padding 16px each; top padding 16px; bottom is the HUD edge (no extra padding).
            float availableW = available.width  - _padding * 2f;
            float availableH = available.height - _padding;
            float usable = Mathf.Min(availableW, availableH);
            if (usable <= 0f)
            {
                Debug.LogWarning("[KingsGridRenderer] _self.rect is zero — Canvas not yet laid out. Falling back to 320px.");
                usable = 320f;
            }

            _cellSize = usable / grid.Size;
            Debug.Log($"[KingsGridRenderer] usable={usable:F1}px  cellSize={_cellSize:F1}px");

            _gridPanel                  = CreatePanel("GridPanel", _self);
            _gridPanel.anchorMin        = new Vector2(0.5f, 0.5f);
            _gridPanel.anchorMax        = new Vector2(0.5f, 0.5f);
            _gridPanel.pivot            = new Vector2(0.5f, 0.5f);
            // Shift down half the top padding so the grid centers within the usable area,
            // not the full GridContainer (which includes the top padding gap).
            _gridPanel.anchoredPosition = new Vector2(0f, -_padding * 0.5f);

            BuildCells(grid);

            float gridTotalSize  = _cellSize * grid.Size;
            _gridPanel.sizeDelta = new Vector2(gridTotalSize, gridTotalSize);
            Debug.Log($"[KingsGridRenderer] RenderGrid complete — gridPanel={gridTotalSize:F0}px, container={_self.rect.size}");
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

            int   size = grid.Size;
            float bt   = _thickBorderWidth;

            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    CellData cell = grid.GetCell(r, c);
                    int      rid  = cell.RegionId;

                    bool boundTop    = r == 0        || grid.GetCell(r - 1, c).RegionId != rid;
                    bool boundBottom = r == size - 1 || grid.GetCell(r + 1, c).RegionId != rid;
                    bool boundLeft   = c == 0        || grid.GetCell(r, c - 1).RegionId != rid;
                    bool boundRight  = c == size - 1 || grid.GetCell(r, c + 1).RegionId != rid;

                    // Container — hit area, coordinate origin, no Image.
                    var cellGo = new GameObject($"Cell_{r}_{c}", typeof(RectTransform));
                    var rt     = cellGo.GetComponent<RectTransform>();
                    rt.SetParent(_gridPanel, false);
                    rt.anchorMin        = new Vector2(0f, 1f);
                    rt.anchorMax        = new Vector2(0f, 1f);
                    rt.pivot            = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(c * _cellSize, -r * _cellSize);
                    rt.sizeDelta        = new Vector2(_cellSize, _cellSize);

                    // Dark background — fills cell and extends bt beyond each region-boundary side.
                    var darkGo = new GameObject("DarkBg", typeof(RectTransform), typeof(Image));
                    var darkRt = darkGo.GetComponent<RectTransform>();
                    darkRt.SetParent(rt, false);
                    darkRt.anchorMin = Vector2.zero;
                    darkRt.anchorMax = Vector2.one;
                    darkRt.offsetMin = new Vector2(boundLeft   ? -bt : 0f, boundBottom ? -bt : 0f);
                    darkRt.offsetMax = new Vector2(boundRight  ?  bt : 0f, boundTop    ?  bt : 0f);
                    var darkImg           = darkGo.GetComponent<Image>();
                    darkImg.color         = _thickBorderColor;
                    darkImg.raycastTarget = false;

                    // Colored foreground — inset to reveal dark behind it.
                    //   Non-boundary side: 0.5f → 1 px total gap between adjacent same-region cells.
                    //   Boundary side:     bt/2 → bt px total dark gap between different-region cells.
                    float thinHalf = _thinBorderWidth * 0.5f;
                    float insetL = boundLeft   ? bt * 0.5f : thinHalf;
                    float insetR = boundRight  ? bt * 0.5f : thinHalf;
                    float insetT = boundTop    ? bt * 0.5f : thinHalf;
                    float insetB = boundBottom ? bt * 0.5f : thinHalf;

                    var fgGo = new GameObject("ColoredFg", typeof(RectTransform), typeof(Image));
                    var fgRt = fgGo.GetComponent<RectTransform>();
                    fgRt.SetParent(rt, false);
                    fgRt.anchorMin = Vector2.zero;
                    fgRt.anchorMax = Vector2.one;
                    fgRt.offsetMin = new Vector2(insetL,  insetB);
                    fgRt.offsetMax = new Vector2(-insetR, -insetT);

                    regionMap.TryGetValue(rid, out RegionData region);
                    Color regionColor = (region != null && region.RegionColor != Color.white)
                        ? region.RegionColor
                        : Color.HSVToRGB((rid * 0.13f) % 1f, 0.45f, 0.85f);

                    var fgImg           = fgGo.GetComponent<Image>();
                    fgImg.color         = regionColor;

                    // Icon — centered inside the foreground.
                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    var iconRt = iconGo.GetComponent<RectTransform>();
                    iconRt.SetParent(fgRt, false);
                    iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
                    iconRt.pivot            = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = Vector2.zero;
                    float iconSize          = _cellSize * IconScale;
                    iconRt.sizeDelta        = new Vector2(iconSize, iconSize);

                    var icon            = iconGo.GetComponent<Image>();
                    icon.preserveAspect = true;
                    icon.raycastTarget  = false;

                    var view = new CellView { Background = fgImg, Icon = icon, BaseColor = regionColor };
                    _cellViews[r, c] = view;
                    ApplyCellState(view, cell.State);

                    var handler      = cellGo.AddComponent<CellEventHandler>();
                    handler.Row      = r;
                    handler.Col      = c;
                    handler.Renderer = this;
                }
            }
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
                    break;
                case CellState.Crown:
                    view.Icon.sprite  = _crownSprite;
                    view.Icon.enabled = _crownSprite != null;
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
