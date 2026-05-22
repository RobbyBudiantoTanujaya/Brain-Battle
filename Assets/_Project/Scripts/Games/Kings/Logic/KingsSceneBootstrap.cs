using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using BrainBattle.Core.Models;
using BrainBattle.Kings;

namespace BrainBattle.Games.Kings.Logic
{
    public sealed class KingsSceneBootstrap : MonoBehaviour
    {
        private const string PendingLevelKey = "Kings_PendingLevel";

        [SerializeField] private LevelLoader      _levelLoader;
        [SerializeField] private KingsGameManager _gameManager;

        public int CurrentLevelNumber { get; private set; }

        private void Start() => StartCoroutine(BootDeferred());

        // Wait one frame so the Canvas CanvasScaler has run and all RectTransforms
        // have their correct world-space sizes before RenderGrid reads them.
        // Reads Kings_PendingLevel from PlayerPrefs (set by LevelSelectController).
        // If the key is absent (e.g. SampleScene launched directly from the Editor),
        // redirect to LevelSelect instead of defaulting to level 1.
        private IEnumerator BootDeferred()
        {
            yield return null;

            if (!PlayerPrefs.HasKey(PendingLevelKey))
            {
                Debug.Log("[KingsSceneBootstrap] No PendingLevel found — redirecting to LevelSelect.");
                SceneManager.LoadScene("LevelSelect");
                yield break;
            }

            int level = PlayerPrefs.GetInt(PendingLevelKey, 1);
            PlayerPrefs.DeleteKey(PendingLevelKey);
            PlayerPrefs.Save();
            Debug.Log($"[KingsSceneBootstrap] BootDeferred — calling LoadLevel({level})");
            LoadLevel(level);
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
