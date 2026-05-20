using UnityEngine;
using BrainBattle.Core.Models;
using BrainBattle.Kings;

namespace BrainBattle.Games.Kings.Logic
{
    public sealed class KingsSceneBootstrap : MonoBehaviour
    {
        [SerializeField] private LevelLoader      _levelLoader;
        [SerializeField] private KingsGameManager _gameManager;

        public int CurrentLevelNumber { get; private set; }

        private void Awake() => LoadLevel(1);

        public void LoadLevel(int levelNumber)
        {
            CurrentLevelNumber = levelNumber;
            LevelData level    = _levelLoader.GetLevel(levelNumber);
            if (level == null)
            {
                Debug.LogError($"[KingsSceneBootstrap] Level {levelNumber} not found.");
                return;
            }

            GridData grid = _levelLoader.BuildGridFromLevel(level);
            _gameManager.StartGame(grid);
        }
    }
}
