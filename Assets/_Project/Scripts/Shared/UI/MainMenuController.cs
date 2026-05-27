using UnityEngine;
using UnityEngine.UI;

namespace BrainBattle.Shared.UI
{
    /// <summary>
    /// Drives the Main Menu screen.
    /// Attach to a root GameObject that has (or contains) a background Image.
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
                _backgroundImage.sprite = null;
                _backgroundImage.color  = DesignSystem.Background;
            }
            else
            {
                Debug.LogWarning("[MainMenuController] No Image component found. " +
                                 "Assign _backgroundImage in the Inspector or attach an Image to this GameObject.");
            }
        }
    }
}
