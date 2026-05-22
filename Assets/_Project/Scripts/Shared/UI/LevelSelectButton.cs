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
        private static readonly Color ColAccent = new Color(0.93f, 0.26f, 0.56f, 1f); // pink
        private static readonly Color ColLocked = new Color(0.12f, 0.12f, 0.18f, 1f); // dark

        [SerializeField] private Image           _background;
        [SerializeField] private TextMeshProUGUI _levelLabel;
        [SerializeField] private GameObject      _checkmark;   // active when Completed
        [SerializeField] private GameObject      _lockOverlay; // active when Locked

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
            if (_levelLabel  != null) _levelLabel.text = DisplayNumber.ToString();

            bool locked    = State == LevelState.Locked;
            bool completed = State == LevelState.Completed;

            if (_background  != null) _background.color     = locked ? ColLocked : ColAccent;
            if (_checkmark   != null) _checkmark.SetActive(completed);
            if (_lockOverlay != null) _lockOverlay.SetActive(locked);
            if (_button      != null) _button.interactable  = !locked;
        }
    }
}
