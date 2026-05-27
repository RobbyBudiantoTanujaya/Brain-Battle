using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BrainBattle.Shared.UI
{
    public enum LevelState { Locked, Available, Completed }

    public sealed class LevelSelectButton : MonoBehaviour
    {
        [SerializeField] private Image _rootImage;
        [SerializeField] private Image _bgImage;
        [SerializeField] private TextMeshProUGUI _primaryText;
        [SerializeField] private TextMeshProUGUI _secondaryText;
        [SerializeField] private LayoutElement _primaryLayout;
        [SerializeField] private LayoutElement _secondaryLayout;
        [SerializeField] private TMP_FontAsset _bodyFont;

        public int LevelNumber { get; private set; }
        public int DisplayNumber { get; private set; }
        public LevelState State { get; private set; }

        public event Action<int> OnLevelSelected;

        private Button _button;

        private static readonly Color ColCompletedBorder = new Color(DesignSystem.Primary.r, DesignSystem.Primary.g, DesignSystem.Primary.b, 0.40f);
        private static readonly Color ColCompletedFill = new Color(DesignSystem.Primary.r, DesignSystem.Primary.g, DesignSystem.Primary.b, 0.16f);
        private static readonly Color ColLockedBorder = new Color(1f, 1f, 1f, 0.14f);
        private static readonly Color ColLockedFill = new Color(1f, 1f, 1f, 0.05f);

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(HandleClick);

            if (_bodyFont == null)
                _bodyFont = Resources.Load<TMP_FontAsset>("Fonts/Outfit SDF");
        }

        public void Setup(int levelNumber, int displayNumber, LevelState state)
        {
            LevelNumber = levelNumber;
            DisplayNumber = displayNumber;
            State = state;
            Refresh();
        }

        private void HandleClick()
        {
            if (State == LevelState.Locked) return;
            OnLevelSelected?.Invoke(LevelNumber);
        }

        private void Refresh()
        {
            switch (State)
            {
                case LevelState.Completed:
                    ApplyCompleted();
                    break;
                case LevelState.Available:
                    ApplyAvailable();
                    break;
                default:
                    ApplyLocked();
                    break;
            }

            if (_button != null)
                _button.interactable = State != LevelState.Locked;
        }

        private void ApplyCompleted()
        {
            if (_rootImage != null)
                _rootImage.color = ColCompletedBorder;

            if (_bgImage != null)
                _bgImage.color = ColCompletedFill;

            if (_primaryText != null)
            {
                _primaryText.text = DisplayNumber.ToString();
                _primaryText.fontSize = 68f;
                _primaryText.fontStyle = FontStyles.Bold;
                _primaryText.color = Color.white;
                if (_bodyFont != null) _primaryText.font = _bodyFont;
            }

            if (_primaryLayout != null)
                _primaryLayout.preferredHeight = 80f;

            if (_secondaryText != null)
            {
                _secondaryText.gameObject.SetActive(true);
                _secondaryText.text = "Completed";
                _secondaryText.fontSize = 22f;
                _secondaryText.fontStyle = FontStyles.Normal;
                _secondaryText.color = new Color(1f, 1f, 1f, 0.60f);
                if (_bodyFont != null) _secondaryText.font = _bodyFont;
            }

            if (_secondaryLayout != null)
                _secondaryLayout.preferredHeight = 30f;
        }

        private void ApplyAvailable()
        {
            if (_rootImage != null)
                _rootImage.color = new Color(0f, 0f, 0f, 0f);

            if (_bgImage != null)
                _bgImage.color = new Color(1f, 0.18f, 0.47f, 1f);

            if (_primaryText != null)
            {
                _primaryText.text = DisplayNumber.ToString();
                _primaryText.fontSize = 68f;
                _primaryText.fontStyle = FontStyles.Bold;
                _primaryText.color = Color.white;
                if (_bodyFont != null) _primaryText.font = _bodyFont;
            }

            if (_primaryLayout != null)
                _primaryLayout.preferredHeight = 80f;

            if (_secondaryText != null)
            {
                _secondaryText.gameObject.SetActive(true);
                _secondaryText.text = "Play";
                _secondaryText.fontSize = 25f;
                _secondaryText.fontStyle = FontStyles.Normal;
                _secondaryText.color = new Color(1f, 1f, 1f, 0.72f);
                if (_bodyFont != null) _secondaryText.font = _bodyFont;
            }

            if (_secondaryLayout != null)
                _secondaryLayout.preferredHeight = 30f;
        }

        private void ApplyLocked()
        {
            if (_rootImage != null)
                _rootImage.color = ColLockedBorder;

            if (_bgImage != null)
                _bgImage.color = ColLockedFill;

            if (_primaryText != null)
            {
                _primaryText.text = DisplayNumber.ToString();
                _primaryText.fontSize = 48f;
                _primaryText.fontStyle = FontStyles.Normal;
                _primaryText.color = new Color(1f, 1f, 1f, 0.20f);
                if (_bodyFont != null) _primaryText.font = _bodyFont;
            }

            if (_primaryLayout != null)
                _primaryLayout.preferredHeight = 56f;

            if (_secondaryText != null)
            {
                _secondaryText.gameObject.SetActive(true);
                _secondaryText.text = "Locked";
                _secondaryText.fontSize = 20f;
                _secondaryText.fontStyle = FontStyles.Normal;
                _secondaryText.color = new Color(1f, 1f, 1f, 0.15f);
                if (_bodyFont != null) _secondaryText.font = _bodyFont;
            }

            if (_secondaryLayout != null)
                _secondaryLayout.preferredHeight = 30f;
        }
    }
}
