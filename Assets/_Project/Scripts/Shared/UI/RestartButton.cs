using UnityEngine;
using UnityEngine.UI;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class RestartButton : MonoBehaviour
    {
        [SerializeField] private KingsGameManager _gameManager;
        [SerializeField] private Button           _button;
        [SerializeField] private GameObject       _confirmPanel;

        private void Awake()
        {
            if (_gameManager   == null) Debug.LogError("[RestartButton] _gameManager not wired. Run BrainBattle → Build Kings Scene.", this);
            if (_button        == null) Debug.LogError("[RestartButton] _button not wired.", this);
            if (_confirmPanel  == null) Debug.LogError("[RestartButton] _confirmPanel not wired.", this);

            if (_button       != null) _button.onClick.AddListener(OnClick);
            if (_confirmPanel != null) _confirmPanel.SetActive(false);
        }

        // Wired to the Confirm button inside _confirmPanel via Inspector onClick.
        public void ConfirmRestart()
        {
            _confirmPanel.SetActive(false);
            _gameManager.RestartGame();
        }

        // Wired to the Cancel button inside _confirmPanel via Inspector onClick.
        public void CancelRestart() => _confirmPanel.SetActive(false);

        private void OnClick()
        {
            AudioManager.Instance?.PlayButtonTap();
            if (_gameManager.MoveCount > 0)
                _confirmPanel.SetActive(true);
            else
                _gameManager.RestartGame();
        }
    }
}
