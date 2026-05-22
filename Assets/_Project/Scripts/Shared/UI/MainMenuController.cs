using UnityEngine;
using UnityEngine.UI;

namespace BrainBattle.Shared.UI
{
    /// <summary>
    /// Drives the Main Menu screen.
    /// Attach to a root GameObject that has (or contains) a background Image.
    /// The background sprite is loaded at runtime from Resources/Sprites/main_menu_bg.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        [SerializeField] private Image _backgroundImage;

        private void Awake()
        {
            // Fall back to an Image on this GameObject if not explicitly wired.
            if (_backgroundImage == null)
                _backgroundImage = GetComponent<Image>();

            if (_backgroundImage != null)
            {
                var sprite = Resources.Load<Sprite>("Sprites/main_menu_bg");
                if (sprite != null)
                {
                    _backgroundImage.sprite = sprite;
                    _backgroundImage.color  = Color.white;
                }
                else
                {
                    Debug.LogWarning("[MainMenuController] 'Sprites/main_menu_bg' not found in Resources. " +
                                     "Ensure the file exists at Assets/_Project/Resources/Sprites/main_menu_bg.png.");
                }
            }
            else
            {
                Debug.LogWarning("[MainMenuController] No Image component found. " +
                                 "Assign _backgroundImage in the Inspector or attach an Image to this GameObject.");
            }
        }
    }
}
