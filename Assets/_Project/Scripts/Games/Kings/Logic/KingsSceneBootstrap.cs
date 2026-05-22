using System.Collections;
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

        private void Start() => StartCoroutine(BootDeferred());

        // Wait one frame so the Canvas CanvasScaler has run and all RectTransforms
        // have their correct world-space sizes before RenderGrid reads them.
        private IEnumerator BootDeferred()
        {
            yield return null;
            Debug.Log("[KingsSceneBootstrap] BootDeferred — calling LoadLevel(1)");
            LoadLevel(1);
        }

        public void LoadLevel(int levelNumber)
        {
            if (_levelLoader == null || _gameManager == null)
            {
                Debug.LogError("[KingsSceneBootstrap] LevelLoader or KingsGameManager reference is null. Check SerializeField wiring.");
                return;
            }

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
