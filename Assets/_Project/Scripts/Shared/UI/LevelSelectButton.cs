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

        [SerializeField] private Image           _background;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private GameObject      _checkmark;   // active when Completed
        [SerializeField] private GameObject      _lockOverlay; // active when Locked

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
            if (_levelLabel != null) _levelLabel.text = DisplayNumber.ToString();

            bool locked    = State == LevelState.Locked;
            bool completed = State == LevelState.Completed;

            if (_background != null)
            {
                // Pick sprite for this state; fall back to solid colour when sprite is null.
                Sprite sprite = locked    ? _spriteLocked    :
                                completed ? _spriteCompleted :
                                            _spriteAvailable;

                if (sprite != null)
                {
                    _background.sprite = sprite;
                    _background.color  = Color.white; // let the sprite's own colours show
                }
                else
                {
                    _background.sprite = null;
                    _background.color  = locked ? ColLocked : ColAccent;
                }
            }

            if (_checkmark   != null) _checkmark.SetActive(completed);
            if (_lockOverlay != null) _lockOverlay.SetActive(locked);
            if (_button      != null) _button.interactable = !locked;
        }
    }
}
