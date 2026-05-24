using UnityEngine;
using UnityEngine.UI;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class UndoButton : MonoBehaviour
    {
        [SerializeField] private KingsGameManager _gameManager;
        [SerializeField] private Button           _button;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            _button.onClick.AddListener(OnClick);
            SetState(false);
        }

        private void OnEnable()
        {
            if (_gameManager != null)
                _gameManager.OnUndoStackChanged += OnUndoStackChanged;
        }

        private void OnDisable()
        {
            if (_gameManager != null)
                _gameManager.OnUndoStackChanged -= OnUndoStackChanged;
        }

        private void OnClick()
        {
            AudioManager.Instance?.PlayButtonTap();
            _gameManager.DoUndo();
        }

        private void OnUndoStackChanged(bool hasUndo) => SetState(hasUndo);

        private void SetState(bool hasUndo)
        {
            _button.interactable  = hasUndo;
            _canvasGroup.alpha    = hasUndo ? 1f : 0.4f;
        }
    }
}
