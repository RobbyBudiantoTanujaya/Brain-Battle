using UnityEngine;
using UnityEngine.UI;
using BrainBattle.Games.Kings.Logic;

namespace BrainBattle.Shared
{
    public sealed class UndoButton : MonoBehaviour
    {
        [SerializeField] private KingsGameManager _gameManager;
        [SerializeField] private Button           _button;

        private void Awake()
        {
            _button.onClick.AddListener(OnClick);
            _button.interactable = false;
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

        private void OnClick() => _gameManager.DoUndo();

        private void OnUndoStackChanged(bool hasUndo) => _button.interactable = hasUndo;
    }
}
