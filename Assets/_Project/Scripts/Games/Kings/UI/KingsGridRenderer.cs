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
        [SerializeField] private float  _padding         = 16f;
        [SerializeField] private float  _borderThickness = 2f;
        [SerializeField] private Color  _borderColor     = new Color(0.15f, 0.15f, 0.15f, 1f);
        [SerializeField] private Color  _conflictColor   = Color.red;
        [SerializeField] private float  _pulseSpeed      = 0.3f;

        /// <summary>Fired when the user taps a cell. Args: (row, col).</summary>
        public event Action<int, int> OnCellTapped;

        private GridData            _currentGrid;
        private RectTransform       _self;
        private RectTransform       _gridPanel;
        private CellView[,]         _cellViews;
        private float               _cellSize;
        private HashSet<Vector2Int> _activeConflicts = new();
        private Coroutine           _conflictCoroutine;

        private sealed class CellView
        {
            public Image Background;
            public Image Icon;
            public Color BaseColor;
        }

        private void Awake() => _self = GetComponent<RectTransform>();

        // ── Public API ────────────────────────────────────────────────────────────

        public void RenderGrid(GridData grid)
        {
            if (grid == null) throw new ArgumentNullException(nameof(grid));

            ClearConflicts();
            DestroyGridPanel();

            _currentGrid = grid;
            _cellViews   = new CellView[grid.Size, grid.Size];

            Canvas.ForceUpdateCanvases();
            Rect available = _self.rect;
            float usable   = Mathf.Min(available.width, available.height) - _padding * 2f;
            if (usable <= 0f) usable = 320f;
            _cellSize = usable / grid.Size;

            _gridPanel                  = CreatePanel("GridPanel", _self);
            _gridPanel.anchorMin        = new Vector2(0.5f, 0.5f);
            _gridPanel.anchorMax        = new Vector2(0.5f, 0.5f);
            _gridPanel.pivot            = new Vector2(0.5f, 0.5f);
            _gridPanel.anchoredPosition = Vector2.zero;
            _gridPanel.sizeDelta        = new Vector2(usable, usable);

            BuildCells(grid);
            BuildBorders(grid);
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

        // ── Grid building ─────────────────────────────────────────────────────────

        private void BuildCells(GridData grid)
        {
            var regionMap = new Dictionary<int, RegionData>(grid.Regions.Count);
            foreach (var region in grid.Regions)
                regionMap[region.RegionId] = region;

            for (int r = 0; r < grid.Size; r++)
            {
                for (int c = 0; c < grid.Size; c++)
                {
                    CellData cell = grid.GetCell(r, c);

                    var cellGo = new GameObject($"Cell_{r}_{c}", typeof(RectTransform), typeof(Image));
                    var rt     = cellGo.GetComponent<RectTransform>();
                    rt.SetParent(_gridPanel, false);
                    rt.anchorMin        = new Vector2(0f, 1f);
                    rt.anchorMax        = new Vector2(0f, 1f);
                    rt.pivot            = new Vector2(0f, 1f);
                    rt.anchoredPosition = new Vector2(c * _cellSize, -r * _cellSize);
                    rt.sizeDelta        = new Vector2(_cellSize, _cellSize);

                    regionMap.TryGetValue(cell.RegionId, out RegionData region);
                    Color baseColor = region?.RegionColor ?? Color.white;

                    var bg   = cellGo.GetComponent<Image>();
                    bg.color = baseColor;

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    var iconRt = iconGo.GetComponent<RectTransform>();
                    iconRt.SetParent(rt, false);
                    iconRt.anchorMin        = new Vector2(0.5f, 0.5f);
                    iconRt.anchorMax        = new Vector2(0.5f, 0.5f);
                    iconRt.pivot            = new Vector2(0.5f, 0.5f);
                    iconRt.anchoredPosition = Vector2.zero;
                    float iconSize          = _cellSize * IconScale;
                    iconRt.sizeDelta        = new Vector2(iconSize, iconSize);

                    var icon            = iconGo.GetComponent<Image>();
                    icon.preserveAspect = true;
                    icon.raycastTarget  = false;

                    var view = new CellView { Background = bg, Icon = icon, BaseColor = baseColor };
                    _cellViews[r, c] = view;
                    ApplyCellState(view, cell.State);

                    // Capture loop variables for the closure.
                    int row = r, col = c;
                    var trigger = cellGo.AddComponent<EventTrigger>();
                    var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                    entry.callback.AddListener(_ => OnCellTapped?.Invoke(row, col));
                    trigger.triggers.Add(entry);
                }
            }
        }

        private void BuildBorders(GridData grid)
        {
            int size = grid.Size;
            for (int r = 0; r < size; r++)
            {
                for (int c = 0; c < size; c++)
                {
                    int regionA = grid.GetCell(r, c).RegionId;

                    // Vertical border to the right of this cell.
                    if (c + 1 < size && regionA != grid.GetCell(r, c + 1).RegionId)
                        SpawnBorderLine(
                            isHorizontal: false,
                            x: (c + 1) * _cellSize,
                            y: -(r * _cellSize + _cellSize * 0.5f),
                            length: _cellSize
                        );

                    // Horizontal border below this cell.
                    if (r + 1 < size && regionA != grid.GetCell(r + 1, c).RegionId)
                        SpawnBorderLine(
                            isHorizontal: true,
                            x: c * _cellSize + _cellSize * 0.5f,
                            y: -(r + 1) * _cellSize,
                            length: _cellSize
                        );
                }
            }
        }

        private void SpawnBorderLine(bool isHorizontal, float x, float y, float length)
        {
            var go = new GameObject(
                isHorizontal ? "BorderH" : "BorderV",
                typeof(RectTransform), typeof(Image));

            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(_gridPanel, false);
            rt.anchorMin        = new Vector2(0f, 1f);
            rt.anchorMax        = new Vector2(0f, 1f);
            rt.pivot            = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta        = isHorizontal
                ? new Vector2(length + _borderThickness, _borderThickness)
                : new Vector2(_borderThickness, length + _borderThickness);

            var img           = go.GetComponent<Image>();
            img.color         = _borderColor;
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
            if (_gridPanel != null)
            {
                Destroy(_gridPanel.gameObject);
                _gridPanel = null;
            }
            _cellViews = null;
        }
    }
}
