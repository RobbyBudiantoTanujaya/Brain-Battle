using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace BrainBattle.Kings
{
    public sealed class TutorialController : MonoBehaviour
    {
        private const string SeenKey = "Kings_TutorialSeen";

        private static readonly string[] Steps =
        {
            "Welcome to Kings! Place one crown in each row.",
            "Each column must also have exactly one crown.",
            "Each colored region must have exactly one crown.",
            "Crowns cannot touch each other, not even diagonally.",
            "Tap a cell to cycle: empty → dot (mark) → crown. Good luck!"
        };

        [SerializeField] private GameObject      _overlayPanel;
        [SerializeField] private TextMeshProUGUI _stepText;
        [SerializeField] private TextMeshProUGUI _stepCounter;
        [SerializeField] private Button          _nextButton;
        [SerializeField] private Button          _skipButton;

        public event System.Action OnTutorialComplete;

        private int              _currentStep;
        private TextMeshProUGUI  _nextLabel;

        private void Awake()
        {
            _nextLabel = _nextButton.GetComponentInChildren<TextMeshProUGUI>();
            _nextButton.onClick.AddListener(OnNext);
            _skipButton.onClick.AddListener(Complete);
            _overlayPanel.SetActive(false);
        }

        /// <summary>
        /// Shows the tutorial overlay from step 1.
        /// Skips silently (fires OnTutorialComplete) if already seen and forceShow is false.
        /// </summary>
        public void ShowTutorial(bool forceShow = false)
        {
            if (!forceShow && PlayerPrefs.GetInt(SeenKey, 0) == 1)
            {
                OnTutorialComplete?.Invoke();
                return;
            }

            _currentStep = 0;
            RefreshUI();
            _overlayPanel.SetActive(true);
        }

        private void OnNext()
        {
            _currentStep++;
            if (_currentStep >= Steps.Length)
            {
                Complete();
                return;
            }
            RefreshUI();
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(SeenKey, 1);
            PlayerPrefs.Save();
            _overlayPanel.SetActive(false);
            OnTutorialComplete?.Invoke();
        }

        private void RefreshUI()
        {
            _stepText.text    = Steps[_currentStep];
            _stepCounter.text = $"{_currentStep + 1} / {Steps.Length}";

            if (_nextLabel != null)
                _nextLabel.text = _currentStep == Steps.Length - 1 ? "Play" : "Next";
        }
    }
}
