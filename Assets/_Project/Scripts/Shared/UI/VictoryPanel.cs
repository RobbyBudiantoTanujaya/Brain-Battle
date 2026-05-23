using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class VictoryPanel : MonoBehaviour
    {
        private const string PrefKeyFormat  = "Kings_Level_{0}_Stars";
        private const string TimeFormat     = "{0:00}:{1:00}";
        private const float  ScaleDuration  = 0.45f;
        private const float  FadeDuration   = 0.25f;

        private static readonly string[] StarLabels = { string.Empty, "★", "★★", "★★★" };

        [SerializeField] private GameObject          _panel;       // VictoryContent (fullscreen)
        [SerializeField] private GameObject          _hud;         // HUD strip — hidden during victory
        [SerializeField] private TextMeshProUGUI     _timeText;
        [SerializeField] private TextMeshProUGUI     _moveCountText;
        [SerializeField] private TextMeshProUGUI     _starRatingText;
        [SerializeField] private Button              _nextLevelButton;
        [SerializeField] private Button              _restartButton;
        [SerializeField] private Button              _mainMenuButton;
        [SerializeField] private KingsGameManager    _gameManager;
        [SerializeField] private KingsSceneBootstrap _sceneBootstrap;

        private CanvasGroup   _canvasGroup;
        private RectTransform _panelRt;

        private void Awake()
        {
            if (_panel          == null) Debug.LogError("[VictoryPanel] _panel not wired. Run BrainBattle → Build Kings Scene.", this);
            if (_gameManager    == null) Debug.LogError("[VictoryPanel] _gameManager not wired. Run BrainBattle → Build Kings Scene.", this);
            if (_sceneBootstrap == null) Debug.LogError("[VictoryPanel] _sceneBootstrap not wired. Run BrainBattle → Build Kings Scene.", this);

            if (_panel == null) return;

            _panelRt     = _panel.GetComponent<RectTransform>();
            _canvasGroup = _panel.GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = _panel.AddComponent<CanvasGroup>();

            _panel.SetActive(false);

            _nextLevelButton.onClick.AddListener(OnNextLevel);
            _restartButton.onClick.AddListener(OnRestart);
            _mainMenuButton.onClick.AddListener(OnMainMenu);
        }

        private void Start() => _panel?.SetActive(false);

        private void OnEnable()
        {
            if (_gameManager != null) _gameManager.OnGameComplete += OnGameComplete;
        }

        private void OnDisable()
        {
            if (_gameManager != null) _gameManager.OnGameComplete -= OnGameComplete;
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

            _hud?.SetActive(false);   // hide game HUD — victory is fullscreen
            _panel.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(AnimateEntrance());
        }

        // ── Button handlers ───────────────────────────────────────────────────────

        private void OnNextLevel()
        {
            _hud?.SetActive(true);
            _panel.SetActive(false);
            if (_sceneBootstrap != null)
            {
                PlayerPrefs.SetInt("Kings_PendingLevel", _sceneBootstrap.CurrentLevelNumber + 1);
                PlayerPrefs.Save();
            }
            SceneManager.LoadScene("SampleScene");
        }

        private void OnRestart()
        {
            _hud?.SetActive(true);
            _panel.SetActive(false);
            _gameManager?.RestartGame();
        }

        private void OnMainMenu()
        {
            SceneManager.LoadScene("LevelSelect");
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
        // Phase 1: fade in (FadeDuration) while scaling up from 0.85 → 1.0
        // Phase 2: bounce settle using EaseOutBack (ScaleDuration)

        private IEnumerator AnimateEntrance()
        {
            _canvasGroup.alpha   = 0f;
            _panelRt.localScale  = Vector3.one * 0.85f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed            += Time.unscaledDeltaTime;
                float t             = Mathf.Clamp01(elapsed / FadeDuration);
                _canvasGroup.alpha  = t;
                _panelRt.localScale = Vector3.one * Mathf.Lerp(0.85f, 1f, EaseOutBack(t));
                yield return null;
            }

            _canvasGroup.alpha   = 1f;
            _panelRt.localScale  = Vector3.one;
        }

        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
