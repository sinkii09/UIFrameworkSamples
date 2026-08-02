using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// The bar under the board: level, move count, undo and restart.
    /// <para>
    /// Lives inside <see cref="BoardView"/> rather than as its own <c>UIView</c> on
    /// <c>UILayer.HUD</c> — the HUD layer sorts at 0 and Screen at 100, so a HUD-layer view would
    /// render underneath the board it is meant to control.
    /// </para>
    /// <para>
    /// A plain MonoBehaviour with no ViewModel of its own. It exposes its buttons and setters and
    /// nothing else; <see cref="BoardView"/> owns every binding, so there is only one place where
    /// board state reaches the UI.
    /// </para>
    /// </summary>
    public sealed class BoardControlBar : MonoBehaviour
    {
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _movesLabel;
        [SerializeField] private Button _undoButton;
        [SerializeField] private Button _restartButton;

        /// <summary>
        /// Subscribes the bar's own widgets to the board's state. Bindings are added to the view's
        /// show-scoped bag, so they are disposed with the view rather than outliving it.
        /// </summary>
        // UnityAction, not Action — BindButton forwards to UnityEvent.AddListener, and the two are
        // not interchangeable there.
        public void Bind(BoardViewModel vm, UnityAction onUndoPressed, ref DisposableBag bag)
        {
            if (_levelLabel != null)
                vm.Level.BindToText(_levelLabel, v => $"Level {v}").AddTo(ref bag);

            if (_movesLabel != null)
                vm.MoveCount.BindToText(_movesLabel, v => $"{v}").AddTo(ref bag);

            if (_undoButton != null)
            {
                vm.CanUndo.BindToInteractable(_undoButton).AddTo(ref bag);
                _undoButton.BindButton(onUndoPressed, ref bag);
            }

            if (_restartButton != null)
                _restartButton.BindButton(vm.OnRestartPressed, ref bag);
        }

        /// <summary>
        /// Locks both buttons while the board is animating or finished. Undo stays keyed to
        /// <see cref="SetUndoAvailable"/> on unlock, so re-enabling cannot resurrect an undo that
        /// has no history behind it.
        /// </summary>
        public void SetInteractable(bool interactable, bool undoAvailable)
        {
            if (_restartButton != null) _restartButton.interactable = interactable;
            if (_undoButton != null) _undoButton.interactable = interactable && undoAvailable;
        }
    }
}
