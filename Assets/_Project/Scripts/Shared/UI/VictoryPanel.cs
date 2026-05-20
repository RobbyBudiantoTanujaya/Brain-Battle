using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class VictoryPanel : MonoBehaviour
    {
        private const string PrefKeyFormat = "Kings_Level_{0}_Stars";
        private const string TimeFormat    = "{0:00}:{1:00}";
        private const float  AnimDuration  = 0.4f;

        // Replace entries here for localization.
        private static readonly string[] StarLabels = { string.Empty, "★", "★★", "★★★" };

        [SerializeField] private GameObject          _panel;
        [SerializeField] private TextMeshProUGUI     _timeText;
        [SerializeField] private TextMeshProUGUI     _moveCountText;
        [SerializeField] private TextMeshProUGUI     _starRatingText;
        [SerializeField] private Button              _nextLevelButton;
        [SerializeField] private Button              _restartButton;
        [SerializeField] private Button              _mainMenuButton;
        [SerializeField] private KingsGameManager    _gameManager;
        [SerializeField] private KingsSceneBootstrap _sceneBootstrap;

        private RectTransform _panelRt;

        private void Awake()
        {
            _panel.SetActive(false);
            _panelRt = _panel.GetComponent<RectTransform>();
            _nextLevelButton.onClick.AddListener(OnNextLevel);
            _restartButton.onClick.AddListener(OnRestart);
            _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void OnEnable()
        {
            if (_gameManager != null)
                _gameManager.OnGameComplete += OnGameComplete;
        }

        private void OnDisable()
        {
            if (_gameManager != null)
                _gameManager.OnGameComplete -= OnGameComplete;
        }

        // ── Event handler ─────────────────────────────────────────────────────────

        private void OnGameComplete(float time, int moveCount)
        {
            int stars = CalculateStars(time, _gameManager.HintsUsed, _gameManager.CurrentGridSize);

            _timeText.text       = FormatTime(time);
            _moveCountText.text  = moveCount.ToString();
            _starRatingText.text = StarLabels[stars];

            if (_sceneBootstrap != null)
                SaveResult(_sceneBootstrap.CurrentLevelNumber, stars);

            _panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateEntrance());
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnNextLevel()
        {
            _panel.SetActive(false);
            _sceneBootstrap?.LoadLevel(_sceneBootstrap.CurrentLevelNumber + 1);
        }

        private void OnRestart()
        {
            _panel.SetActive(false);
            _gameManager?.RestartGame();
        }

        private void OnMainMenu()
        {
            // Wire to scene load when main menu is implemented (Milestone 2+).
        }

        // ── Star rating ───────────────────────────────────────────────────────────

        private static int CalculateStars(float time, int hintsUsed, int gridSize)
        {
            bool withinTime = time < GetParTime(gridSize);
            bool noHints    = hintsUsed == 0;

            if (noHints && withinTime)   return 3;
            if (!noHints && !withinTime) return 1;
            return 2;
        }

        private static int GetParTime(int gridSize) => gridSize switch
        {
            4  => 60,
            5  => 90,
            6  => 120,
            8  => 180,
            10 => 300,
            _  => 120
        };

        // ── Persistence ───────────────────────────────────────────────────────────

        private static void SaveResult(int levelNumber, int stars)
        {
            string key  = string.Format(PrefKeyFormat, levelNumber);
            int    best = PlayerPrefs.GetInt(key, 0);
            if (stars <= best) return;
            PlayerPrefs.SetInt(key, stars);
            PlayerPrefs.Save();
        }

        // ── Formatting ────────────────────────────────────────────────────────────

        private static string FormatTime(float totalSeconds)
        {
            int minutes = (int)totalSeconds / 60;
            int seconds = (int)totalSeconds % 60;
            return string.Format(TimeFormat, minutes, seconds);
        }

        // ── Animation ─────────────────────────────────────────────────────────────

        private IEnumerator AnimateEntrance()
        {
            float elapsed       = 0f;
            _panelRt.localScale = Vector3.zero;

            while (elapsed < AnimDuration)
            {
                elapsed            += Time.unscaledDeltaTime;
                float t             = Mathf.Clamp01(elapsed / AnimDuration);
                _panelRt.localScale = Vector3.one * EaseOutBack(t);
                yield return null;
            }

            _panelRt.localScale = Vector3.one;
        }

        // Ease-out-back: slight overshoot then settle, giving a bouncy panel entrance.
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
