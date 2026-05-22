using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace BrainBattle.Shared.UI
{
    /// <summary>
    /// Drives the Level Select screen: tab switching, progress display, level grid, and Play button.
    /// The scene is created by BrainBattle/Build Level Select Scene editor tool.
    /// </summary>
    public sealed class LevelSelectController : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────────────

        private const string GameSceneName   = "SampleScene";
        private const string PendingLevelKey = "Kings_PendingLevel";
        private const string StarsKeyFmt     = "Kings_Level_{0}_Stars";

        /// <summary>Level numbers that belong to each difficulty tab (Beginner / Expert / Impossible).</summary>
        private static readonly int[][] DiffPools =
        {
            new[] { 1,  2,  3,  4,  5,  6,  7,  8,  9, 10, 11, 12 },         // Beginner   – 4×4, 5×5
            new[] { 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 },         // Expert     – 6×6, 8×8
            new[] { 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35 },             // Impossible – 10×10
        };

        private static readonly Color ColTabActive   = new Color(0.93f, 0.26f, 0.56f, 1f);
        private static readonly Color ColTabInactive = new Color(0.12f, 0.12f, 0.18f, 1f);

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Tabs (length 3)")]
        [SerializeField] private Button[]          _tabButtons;    // Beginner | Expert | Impossible
        [SerializeField] private Image[]           _progressFills; // Filled-type Images for progress bars
        [SerializeField] private TextMeshProUGUI[] _progressTexts; // "X%" labels

        [Header("Level Grid")]
        [SerializeField] private Transform  _gridContent;
        [SerializeField] private GameObject _levelButtonPrefab;

        [Header("Bottom")]
        [SerializeField] private Button _playButton;

        // ── State ─────────────────────────────────────────────────────────────────

        private int _activeTab;
        private readonly List<LevelSelectButton> _buttons = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                int idx = i; // capture for lambda
                _tabButtons[i].onClick.AddListener(() => SelectTab(idx));
            }
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlay);
        }

        private void Start() => SelectTab(0);

        // ── Tab switching ─────────────────────────────────────────────────────────

        private void SelectTab(int index)
        {
            _activeTab = index;

            // Highlight active tab, dim others.
            for (int i = 0; i < _tabButtons.Length; i++)
            {
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = (i == index) ? ColTabActive : ColTabInactive;
            }

            RefreshProgress();
            RebuildGrid(index);
        }

        // ── Progress bars ─────────────────────────────────────────────────────────

        private void RefreshProgress()
        {
            for (int d = 0; d < DiffPools.Length; d++)
            {
                int[] pool = DiffPools[d];
                int   done = 0;
                foreach (int lvl in pool)
                    if (Stars(lvl) > 0) done++;

                float pct = pool.Length > 0 ? (float)done / pool.Length : 0f;

                if (_progressFills  != null && d < _progressFills.Length  && _progressFills[d]  != null)
                    _progressFills[d].fillAmount = pct;
                if (_progressTexts  != null && d < _progressTexts.Length  && _progressTexts[d]  != null)
                    _progressTexts[d].text = $"{Mathf.RoundToInt(pct * 100)}%";
            }
        }

        // ── Level grid ────────────────────────────────────────────────────────────

        private void RebuildGrid(int diffIndex)
        {
            foreach (var b in _buttons)
                if (b != null) Destroy(b.gameObject);
            _buttons.Clear();

            if (_gridContent == null || _levelButtonPrefab == null) return;

            int[] pool = DiffPools[diffIndex];
            for (int i = 0; i < pool.Length; i++)
            {
                int lvl         = pool[i];
                int displayNum  = i + 1; // show 1-based index within the tab

                var go  = Instantiate(_levelButtonPrefab, _gridContent);
                var btn = go.GetComponent<LevelSelectButton>();
                if (btn == null) continue;

                btn.Setup(lvl, displayNum, StateFor(lvl));
                btn.OnLevelSelected += GoToLevel;
                _buttons.Add(btn);
            }
        }

        // ── State helpers ─────────────────────────────────────────────────────────

        private static LevelState StateFor(int lvl)
        {
            // Level 1 is always unlocked.
            if (lvl <= 1) return Stars(1) > 0 ? LevelState.Completed : LevelState.Available;
            // Locked if the previous level has never been completed.
            if (Stars(lvl - 1) == 0) return LevelState.Locked;
            return Stars(lvl) > 0 ? LevelState.Completed : LevelState.Available;
        }

        private static int Stars(int lvl) =>
            PlayerPrefs.GetInt(string.Format(StarsKeyFmt, lvl), 0);

        // ── Navigation ────────────────────────────────────────────────────────────

        private void GoToLevel(int lvl)
        {
            PlayerPrefs.SetInt(PendingLevelKey, lvl);
            PlayerPrefs.Save();
            SceneManager.LoadScene(GameSceneName);
        }

        /// <summary>
        /// Loads the first available (incomplete) level in the active tab.
        /// Falls back to the last completed level if all are done.
        /// </summary>
        private void OnPlay()
        {
            int[] pool   = DiffPools[_activeTab];
            int   target = pool.Length > 0 ? pool[0] : 1;

            foreach (int lvl in pool)
            {
                if (StateFor(lvl) == LevelState.Available) { target = lvl; break; }
            }

            GoToLevel(target);
        }
    }
}
