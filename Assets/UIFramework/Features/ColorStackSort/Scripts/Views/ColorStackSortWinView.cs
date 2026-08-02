using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// The level-complete panel. Popup layer, so it draws above the board (Screen).
    /// </summary>
    /// <remarks>
    /// The key is namespaced because UIViewRegistry scans every assembly and drops a second view
    /// sharing a key — an unqualified "WinView" would collide with MemoryGame's.
    /// <para>
    /// This view is never dismissed with <c>HideAsync&lt;T&gt;</c>. That call returns silently while
    /// the navigator is transitioning, which would strand the popup over a board whose raycaster is
    /// disabled. It goes away only via the <c>CloseAllAsync</c> inside
    /// <c>RestartCurrentStateAsync</c>.
    /// </para>
    /// </remarks>
    [Preserve]
    [UIViewKey("ColorStackSort/ColorStackSortWinView")]
    public sealed class ColorStackSortWinView : UIView<ColorStackSortWinViewModel>
    {
        public override UILayer Layer => UILayer.Popup;

        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _titleLabel;
        [SerializeField] private TMP_Text _movesLabel;
        [SerializeField] private Button _nextButton;

        [Tooltip("Optional. Fires once as the panel scales in.")]
        [SerializeField] private JuiceBurstEmitter _confetti;

        private static readonly Color ConfettiTint = new Color(1f, 0.86f, 0.35f, 1f);

        private Sequence _showSequence;

        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            CanvasGroup.alpha = 0f;
            if (_panel != null) _panel.localScale = Vector3.zero;
        }

        protected override void BindViewModel(ColorStackSortWinViewModel vm)
        {
            if (_titleLabel != null)
                vm.Level.BindToText(_titleLabel, v => $"Level {v} complete").AddTo(ref _showDisposables);

            if (_movesLabel != null)
                vm.MoveCount.BindToText(_movesLabel, v => $"{v} moves").AddTo(ref _showDisposables);

            if (_nextButton == null)
            {
                Debug.LogError("[ColorStackSortWinView] Next button not assigned — the level cannot " +
                               "be advanced. Delete this prefab and re-run Tools/ColorStackSort/Build Prefabs.", this);
                return;
            }

            // Interactability is entirely the ViewModel's call: false until the entrance animation
            // completes, then false again permanently on the first press.
            vm.CanAdvance.BindToInteractable(_nextButton).AddTo(ref _showDisposables);
            _nextButton.BindButton(vm.OnNextPressed, ref _showDisposables);
        }

        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            CanvasGroup.alpha = 0f;

            // Fired here rather than after the await: it should land WITH the panel, and a cancelled
            // entrance never reaches the code below. Stopping it is not this view's job — the
            // emitter clears itself in its own OnDisable, which runs when this view deactivates,
            // including on the cancelled path (ShowAsync catches the cancellation and resets
            // IsVisible/SetActive).
            if (_confetti != null) _confetti.Burst(ConfettiTint);

            _showSequence = DOTween.Sequence().SetLink(gameObject);
            _showSequence.Join(CanvasGroup.DOFade(1f, 0.2f));

            if (_panel != null)
                _showSequence.Join(_panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 1.6f));

            try
            {
                await _showSequence.AwaitAsync(ct);
            }
            finally
            {
                // In a finally: a cancelled AwaitAsync would otherwise leave a killed, possibly
                // recycled tween here for OnPrepareForShow to Kill() again on the next show.
                _showSequence = null;
            }

            // Only on the success path. Arming the button after a cancelled entrance would enable
            // input on a view that is being torn down.
            ViewModel.MarkReadyForInput();
        }

        private void OnDisable()
        {
            _showSequence?.Kill();
            _showSequence = null;
            CanvasGroup.DOKill();
            if (_panel != null) _panel.DOKill();
        }
    }
}
