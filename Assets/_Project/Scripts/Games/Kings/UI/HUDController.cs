using UnityEngine;
using TMPro;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Games.Kings.UI
{
    public sealed class HUDController : MonoBehaviour
    {
        [SerializeField] private KingsGameManager _gameManager;
        [SerializeField] private TextMeshProUGUI  _timerText;
        [SerializeField] private TextMeshProUGUI  _moveCountText;

        private void Update()
        {
            if (_gameManager == null) return;

            float t       = _gameManager.ElapsedSeconds;
            int   minutes = (int)t / 60;
            int   seconds = (int)t % 60;

            if (_timerText    != null) _timerText.text    = $"{minutes:00}:{seconds:00}";
            if (_moveCountText != null) _moveCountText.text = _gameManager.MoveCount.ToString();
        }
    }
}
