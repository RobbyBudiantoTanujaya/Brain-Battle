using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class TipsButton : MonoBehaviour
    {
        private const float  CooldownSeconds = 10f;
        private const int    MaxHints        = 3;
        private const string NoHintsText     = "No hints available right now. Keep trying!";
        private const string CooldownText    = "Please wait before requesting another hint.";

        [SerializeField] private KingsGameManager _gameManager;
        [SerializeField] private Button           _button;
        [SerializeField] private GameObject       _tipsPanel;
        [SerializeField] private TextMeshProUGUI  _tipsText;
        [SerializeField] private Button           _closeButton;

        private float _lastTipTime = float.MinValue;

        private void Awake()
        {
            if (_gameManager  == null) Debug.LogError("[TipsButton] _gameManager not wired. Run BrainBattle → Build Kings Scene.", this);
            if (_button       == null) Debug.LogError("[TipsButton] _button not wired.", this);
            if (_tipsPanel    == null) Debug.LogError("[TipsButton] _tipsPanel not wired.", this);
            if (_tipsText     == null) Debug.LogError("[TipsButton] _tipsText not wired.", this);
            if (_closeButton  == null) Debug.LogError("[TipsButton] _closeButton not wired.", this);

            if (_button      != null) _button.onClick.AddListener(OnClick);
            if (_closeButton != null) _closeButton.onClick.AddListener(ClosePanel);
            if (_tipsPanel   != null) _tipsPanel.SetActive(false);
        }

        // Wired to the close button inside _tipsPanel.
        public void ClosePanel() => _tipsPanel.SetActive(false);

        private void OnClick()
        {
            if (Time.time - _lastTipTime < CooldownSeconds)
            {
                ShowPanel(CooldownText);
                return;
            }

            var hints = _gameManager.GetHints();
            if (hints == null || hints.Count == 0)
            {
                ShowPanel(NoHintsText);
                return;
            }

            int count = Mathf.Min(hints.Count, MaxHints);
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < count; i++)
            {
                if (i > 0) sb.AppendLine();
                sb.Append($"{i + 1}. {hints[i]}");
            }

            _lastTipTime = Time.time;
            ShowPanel(sb.ToString());
        }

        private void ShowPanel(string text)
        {
            _tipsText.text = text;
            _tipsPanel.SetActive(true);
        }
    }
}
