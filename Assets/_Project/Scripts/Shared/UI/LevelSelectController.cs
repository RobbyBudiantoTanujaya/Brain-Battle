using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BrainBattle.Kings;
using BrainBattle.Shared;
using BrainBattle.Shared.UI;

namespace BrainBattle.Shared.UI
{
    public sealed class LevelSelectController : MonoBehaviour
    {
        private const string GameSceneName   = "SampleScene";
        private const string PendingLevelKey = "Kings_PendingLevel";
        private const string StarsKeyFmt     = "Kings_Level_{0}_Stars";

        private static readonly string[] DifficultyNames = { "Beginner", "Expert", "Impossible" };

        private static readonly Color ColTabActive      = DesignSystem.Primary;
        private static readonly Color ColTabInactive    = DesignSystem.Surface;
        private static readonly Color ColProgressFill   = DesignSystem.Primary;
        private static readonly Color ColTabTxtActive   = DesignSystem.TextPrimary;
        private static readonly Color ColTabTxtInactive = DesignSystem.TextSecondary;

        // ── Inspector ─────────────────────────────────────────────────────────────

        [Header("Level Data")]
        [SerializeField] private LevelData[] _allLevels;

        [Header("Tabs (length 3)")]
        [SerializeField] private Button[] _tabButtons;

        [Header("Progress")]
        [SerializeField] private Image _progressFill;
        [SerializeField] private TextMeshProUGUI _progressText;
        [SerializeField] private TextMeshProUGUI _progressLabel;

        [Header("Level Grid")]
        [SerializeField] private Transform  _gridContent;
        [SerializeField] private GameObject _levelButtonPrefab;

        [Header("Bottom")]
        [SerializeField] private Button _playButton;

        // ── State ─────────────────────────────────────────────────────────────────

        private int _activeTab = -1;
        private int[][] _diffPools;
        private readonly List<LevelSelectButton> _buttons = new();

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (_tabButtons == null || _tabButtons.Length == 0 || _tabButtons[0] == null)
                TryFindTabButtonsFallback();

            if (_tabButtons != null)
            {
                for (int i = 0; i < _tabButtons.Length; i++)
                {
                    if (_tabButtons[i] == null) continue;
                    int idx = i;
                    _tabButtons[i].onClick.AddListener(() =>
                    {
                        AudioManager.Instance?.PlayButtonTap();
                        SelectTab(idx);
                    });
                }
            }
            if (_playButton != null)
                _playButton.onClick.AddListener(OnPlay);
        }

        private void TryFindTabButtonsFallback()
        {
            string[] names = { "BeginnerTab", "ExpertTab", "ImpossibleTab" };
            _tabButtons    = new Button[3];
            for (int i = 0; i < names.Length; i++)
            {
                var go = GameObject.Find(names[i]);
                if (go != null) _tabButtons[i] = go.GetComponent<Button>();
            }
            Debug.LogWarning("[LevelSelectController] _tabButtons were null — resolved via fallback. " +
                             "Re-run BrainBattle/Build Level Select Scene to fix wiring permanently.");
        }

        private void Start()
        {
            BuildDiffPools();
            SelectTab(0);
        }

        // ── Pool builder ──────────────────────────────────────────────────────────

        // Groups _allLevels by Difficulty, sorted by LevelNumber ascending.
        // Called once on Start — no rebuild needed at runtime.
        private void BuildDiffPools()
        {
            var pools = new List<int>[DifficultyNames.Length];
            for (int i = 0; i < pools.Length; i++) pools[i] = new List<int>();

            if (_allLevels != null && _allLevels.Length > 0)
            {
                var sorted = (LevelData[])_allLevels.Clone();
                System.Array.Sort(sorted, (a, b) => a.LevelNumber.CompareTo(b.LevelNumber));

                foreach (var level in sorted)
                {
                    if (level == null) continue;
                    for (int d = 0; d < DifficultyNames.Length; d++)
                    {
                        if (level.Difficulty == DifficultyNames[d])
                        {
                            pools[d].Add(level.LevelNumber);
                            break;
                        }
                    }
                }
            }

            _diffPools = new int[DifficultyNames.Length][];
            for (int i = 0; i < DifficultyNames.Length; i++)
                _diffPools[i] = pools[i].ToArray();

            Debug.Log($"[LevelSelectController] Pools built — " +
                      $"Beginner:{_diffPools[0].Length} Expert:{_diffPools[1].Length} " +
                      $"Impossible:{_diffPools[2].Length}");
        }

        // ── Tab switching ─────────────────────────────────────────────────────────

        private void SelectTab(int index)
        {
            if (index == _activeTab)
                return;

            _activeTab = index;

            for (int i = 0; i < _tabButtons.Length; i++)
            {
                if (_tabButtons[i] == null) continue;
                bool active = (i == index);
                var img = _tabButtons[i].GetComponent<Image>();
                if (img != null) img.color = active ? ColTabActive : ColTabInactive;

                var lbl = _tabButtons[i].GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null)
                {
                    lbl.color     = active ? ColTabTxtActive   : ColTabTxtInactive;
                    lbl.fontStyle = active ? FontStyles.Bold   : FontStyles.Normal;
                }
            }

            RefreshProgress();
            RebuildGrid(index);
        }

        // ── Progress bars ─────────────────────────────────────────────────────────

        private void RefreshProgress()
        {
            if (_diffPools == null || _activeTab < 0 || _activeTab >= _diffPools.Length) return;

            int[] pool = _diffPools[_activeTab];
            int done = 0;
            foreach (int lvl in pool)
                if (Stars(lvl) > 0) done++;

            float pct = pool.Length > 0 ? (float)done / pool.Length : 0f;

            if (_progressFill != null)
            {
                _progressFill.fillAmount = pct;
                _progressFill.color = ColProgressFill;
            }

            if (_progressText != null)
                _progressText.text = $"{Mathf.RoundToInt(pct * 100)}%";

            if (_progressLabel != null)
                _progressLabel.text = $"{DifficultyNames[_activeTab]} progress";
        }

        // ── Level grid ────────────────────────────────────────────────────────────

        private void RebuildGrid(int diffIndex)
        {
            foreach (var b in _buttons)
                if (b != null) Destroy(b.gameObject);
            _buttons.Clear();

            if (_gridContent == null || _levelButtonPrefab == null) return;
            if (_diffPools == null || diffIndex >= _diffPools.Length) return;

            int[] pool = _diffPools[diffIndex];
            for (int i = 0; i < pool.Length; i++)
            {
                int lvl        = pool[i];
                int displayNum = i + 1;

                var go  = Instantiate(_levelButtonPrefab, _gridContent);
                var btn = go.GetComponent<LevelSelectButton>();
                if (btn == null) continue;

                btn.Setup(lvl, displayNum, StateFor(lvl, pool));
                btn.OnLevelSelected += GoToLevel;
                _buttons.Add(btn);
            }
        }

        // ── State helpers ─────────────────────────────────────────────────────────

        private static LevelState StateFor(int lvl, int[] pool)
        {
            bool isFirstInPool = pool.Length > 0 && pool[0] == lvl;
            if (isFirstInPool)
                return Stars(lvl) > 0 ? LevelState.Completed : LevelState.Available;

            int prevInPool = -1;
            for (int i = 1; i < pool.Length; i++)
            {
                if (pool[i] == lvl) { prevInPool = pool[i - 1]; break; }
            }

            if (prevInPool < 0 || Stars(prevInPool) == 0) return LevelState.Locked;
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

        private void OnPlay()
        {
            AudioManager.Instance?.PlayButtonTap();
            if (_diffPools == null || _activeTab >= _diffPools.Length) return;
            int[] pool   = _diffPools[_activeTab];
            int   target = pool.Length > 0 ? pool[0] : 1;

            foreach (int lvl in pool)
            {
                if (StateFor(lvl, pool) == LevelState.Available) { target = lvl; break; }
            }

            GoToLevel(target);
        }
    }
}
