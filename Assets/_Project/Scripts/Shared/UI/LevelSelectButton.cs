using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainBattle.Shared.UI
{
    public enum LevelState { Locked, Available, Completed }

    /// <summary>
    /// Single cell in the Level Select grid.
    /// Call Setup() after instantiation to configure number + state.
    /// Fires OnLevelSelected when tapped (Locked state is silently ignored).
    /// </summary>
    public sealed class LevelSelectButton : MonoBehaviour
    {
        // Fallback solid colours used when sprites are unavailable.
        private static readonly Color ColAccent = new Color(0.93f, 0.26f, 0.56f, 1f); // pink
        private static readonly Color ColLocked = new Color(0.12f, 0.12f, 0.18f, 1f); // dark

        [SerializeField] private Image           _background;   // root Image — always transparent (raycast target)
        [SerializeField] private Image           _spriteImage;  // child Image — fills button, shows state sprite
        [SerializeField] private TextMeshProUGUI _levelLabel;   // bottom-centre number; hidden when Locked
        [SerializeField] private GameObject      _checkmark;    // active when Completed
        [SerializeField] private GameObject      _lockOverlay;  // legacy — kept for prefab compat, always hidden

        [Header("Sprites (auto-loaded from Resources if not assigned)")]
        [SerializeField] private Sprite _spriteAvailable;
        [SerializeField] private Sprite _spriteCompleted;
        [SerializeField] private Sprite _spriteActive;   // reserved for current/highlighted level
        [SerializeField] private Sprite _spriteLocked;

        public int        LevelNumber  { get; private set; }
        public int        DisplayNumber { get; private set; }
        public LevelState State        { get; private set; }

        public event Action<int> OnLevelSelected;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button != null)
                _button.onClick.AddListener(HandleClick);

            // Load sprites from Resources if not pre-assigned via Inspector / SceneBuilder.
            if (_spriteAvailable == null) _spriteAvailable = Resources.Load<Sprite>("Sprites/level_available");
            if (_spriteCompleted == null) _spriteCompleted = Resources.Load<Sprite>("Sprites/level_completed");
            if (_spriteActive    == null) _spriteActive    = Resources.Load<Sprite>("Sprites/level_active");
            if (_spriteLocked    == null) _spriteLocked    = Resources.Load<Sprite>("Sprites/level_lock");
        }

        /// <summary>
        /// Configure the button.
        /// <paramref name="levelNumber"/> is the global level number used for PlayerPrefs and navigation.
        /// <paramref name="displayNumber"/> is the 1-based index shown in the UI label (e.g. 1-12 per tab).
        /// </summary>
        public void Setup(int levelNumber, int displayNumber, LevelState state)
        {
            LevelNumber   = levelNumber;
            DisplayNumber = displayNumber;
            State         = state;
            Refresh();
        }

        private void HandleClick()
        {
            if (State == LevelState.Locked) return;
            OnLevelSelected?.Invoke(LevelNumber);
        }

        private void Refresh()
        {
            bool locked    = State == LevelState.Locked;
            bool completed = State == LevelState.Completed;

            // Root Image is always transparent — it only exists as a Button raycast target.
            if (_background != null)
            {
                _background.sprite = null;
                _background.color  = Color.clear;
            }

            // Sprite Image fills the entire button and shows the correct state art.
            if (_spriteImage != null)
            {
                Sprite sprite = locked    ? _spriteLocked    :
                                completed ? _spriteCompleted :
                                            _spriteAvailable;
                _spriteImage.sprite          = sprite;
                _spriteImage.color           = Color.white;
                _spriteImage.preserveAspect  = false;
            }

            // Level number: white text, visible only for Available + Completed.
            if (_levelLabel != null)
            {
                _levelLabel.text = DisplayNumber.ToString();
                _levelLabel.gameObject.SetActive(!locked);
            }

            if (_checkmark   != null) _checkmark.SetActive(completed);
            if (_lockOverlay != null) _lockOverlay.SetActive(false); // sprite handles locked state
            if (_button      != null) _button.interactable = !locked;
        }
    }
}
