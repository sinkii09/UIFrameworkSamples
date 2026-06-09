using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    public class WinView : UIView<WinViewModel>
    {
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private TMP_Text _timeText;
        [SerializeField] private Button _playAgainButton;
        [SerializeField] private Button _mainMenuButton;

        [Header("Animation")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text _titleText;

        public override UILayer Layer => UILayer.Popup;

        private Sequence _showSequence;
        private Sequence _hideSequence;

        // Called before SetActive(true) — reset everything so the first frame after activation
        // shows no content (panel and elements at scale 0, overlay fully transparent).
        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            CanvasGroup.alpha = 0f;
            if (_panel != null) _panel.localScale = Vector3.zero;
            if (_titleText != null) _titleText.transform.localScale = Vector3.zero;
            _movesText.transform.localScale = Vector3.zero;
            _timeText.transform.localScale = Vector3.zero;
            _playAgainButton.transform.localScale = Vector3.zero;
            _mainMenuButton.transform.localScale = Vector3.zero;
        }

        protected override void BindViewModel(WinViewModel vm)
        {
            vm.MovesText.BindToText(_movesText).AddTo(ref _showDisposables);
            vm.TimeText.BindToText(_timeText).AddTo(ref _showDisposables);
            _playAgainButton.BindButton(vm.OnPlayAgain, ref _showDisposables);
            _mainMenuButton.BindButton(vm.OnMainMenu, ref _showDisposables);
        }

        // Overlay fades in while the panel bounces up from scale 0.
        // Inner elements stagger in after the panel has mostly settled, creating a layered entrance.
        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            // UIAnimator instantly sets alpha=1 when _showTransition is null.
            // Reset to 0 so our fade-in is visible.
            CanvasGroup.alpha = 0f;

            _showSequence = DOTween.Sequence().SetLink(gameObject);

            _showSequence.Join(CanvasGroup.DOFade(1f, 0.2f));
            if (_panel != null)
                _showSequence.Join(_panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

            if (_titleText != null)
                _showSequence.Insert(0.2f, _titleText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.28f, _movesText.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.35f, _timeText.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.43f, _playAgainButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.51f, _mainMenuButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

            await _showSequence.AwaitAsync(ct);
            _showSequence = null;
        }

        // Tiny scale-up pop acknowledges the dismiss, then panel shrinks to 0 while overlay fades.
        protected override async UniTask OnHideAsync(CancellationToken ct)
        {
            // UIAnimator instantly sets alpha=0 when _hideTransition is null.
            // Restore to 1 so our fade-out animation is visible.
            CanvasGroup.alpha = 1f;

            _hideSequence = DOTween.Sequence().SetLink(gameObject);
            if (_panel != null)
            {
                _hideSequence.Append(_panel.DOScale(1.08f, 0.08f).SetEase(Ease.OutQuad));
                _hideSequence.Append(_panel.DOScale(0f, 0.22f).SetEase(Ease.InBack));
            }
            // Fade covers the full duration so the overlay disappears in sync with the panel exit.
            _hideSequence.Insert(0f, CanvasGroup.DOFade(0f, 0.3f));
            await _hideSequence.AwaitAsync(ct);
            _hideSequence = null;
        }

        private void OnDisable()
        {
            _showSequence?.Kill();
            _showSequence = null;
            _hideSequence?.Kill();
            _hideSequence = null;
            CanvasGroup.DOKill();
            if (_panel != null) _panel.DOKill();
            if (_titleText != null) _titleText.transform.DOKill();
            if (_movesText != null) _movesText.transform.DOKill();
            if (_timeText != null) _timeText.transform.DOKill();
            if (_playAgainButton != null) _playAgainButton.transform.DOKill();
            if (_mainMenuButton != null) _mainMenuButton.transform.DOKill();
        }
    }
}
