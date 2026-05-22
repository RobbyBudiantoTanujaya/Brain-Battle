using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace BrainBattle.Shared.UI
{
    /// <summary>
    /// HUD button that navigates back to the Level Select screen.
    /// Attach to a Button GameObject in the HUD.
    /// </summary>
    public sealed class MenuButton : MonoBehaviour
    {
        private const string LevelSelectScene = "LevelSelect";

        [SerializeField] private Button _button;

        private void Awake()
        {
            if (_button == null)
                _button = GetComponent<Button>();

            if (_button != null)
                _button.onClick.AddListener(GoToLevelSelect);
        }

        private void GoToLevelSelect()
        {
            // Clear pending level so LevelSelect starts fresh (not the game scene).
            PlayerPrefs.DeleteKey("Kings_PendingLevel");
            PlayerPrefs.Save();
            SceneManager.LoadScene(LevelSelectScene);
        }
    }
}
