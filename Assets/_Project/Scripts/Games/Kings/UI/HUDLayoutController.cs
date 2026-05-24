using UnityEngine;
using TMPro;

namespace BrainBattle.Games.Kings.UI
{
    /// <summary>
    /// Applies responsive HUD font sizing at runtime.
    /// Uses Start() so the Canvas layout system has finished computing
    /// RectTransform sizes before we read rect.height.
    /// HUD height is driven purely by anchor (12% of canvas) set in
    /// KingsSceneBuilder — no sizeDelta manipulation needed here.
    /// </summary>
    public sealed class HUDLayoutController : MonoBehaviour
    {
        // ── SerializeField references (wired by KingsSceneBuilder) ────────────

        [SerializeField] private TextMeshProUGUI _undoIcon;
        [SerializeField] private TextMeshProUGUI _undoLabel;
        [SerializeField] private TextMeshProUGUI _restartIcon;
        [SerializeField] private TextMeshProUGUI _restartLabel;
        [SerializeField] private TextMeshProUGUI _menuIcon;
        [SerializeField] private TextMeshProUGUI _menuLabel;
        [SerializeField] private TextMeshProUGUI _tipsIcon;
        [SerializeField] private TextMeshProUGUI _tipsLabel;
        [SerializeField] private TextMeshProUGUI _moveCountNum;
        [SerializeField] private TextMeshProUGUI _movesLabel;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        private void Start()
        {
            // Read the actual pixel height after the Canvas layout pass.
            float hudH = GetComponent<RectTransform>().rect.height;

            float iconSize    = Mathf.Clamp(hudH * 0.35f, 18f, 26f);
            float labelSize   = Mathf.Clamp(hudH * 0.14f,  9f, 12f);
            float counterSize = Mathf.Clamp(hudH * 0.45f, 20f, 30f);
            float movesSize   = Mathf.Clamp(hudH * 0.11f,  7f,  9f);

            Debug.Log($"[HUDLayoutController] Screen={Screen.width}x{Screen.height} " +
                      $"hudH={hudH:F1}px  icon={iconSize:F1}  label={labelSize:F1}  " +
                      $"counter={counterSize:F1}  moves={movesSize:F1}");

            SetFS(_undoIcon,    iconSize);
            SetFS(_restartIcon, iconSize);
            SetFS(_menuIcon,    iconSize);
            SetFS(_tipsIcon,    iconSize);

            SetFS(_undoLabel,    labelSize);
            SetFS(_restartLabel, labelSize);
            SetFS(_menuLabel,    labelSize);
            SetFS(_tipsLabel,    labelSize);

            SetFS(_moveCountNum, iconSize);
            SetFS(_movesLabel,   labelSize);
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static void SetFS(TextMeshProUGUI tmp, float size)
        {
            if (tmp != null) tmp.fontSize = size;
        }
    }
}
