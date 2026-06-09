using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Sinkii09.UIFramework;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    public class SettingsView : UIView<SettingsViewModel>
    {
        [SerializeField] private Toggle _sfxToggle;
        [SerializeField] private Toggle _musicToggle;
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private RectTransform _panel;

        public override UILayer Layer => UILayer.Popup;

        private Sequence _showSequence;
        private Sequence _hideSequence;

        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            CanvasGroup.alpha = 0f;
            if (_panel != null) _panel.localScale = Vector3.zero;
        }

        protected override void BindViewModel(SettingsViewModel vm)
        {
            _sfxToggle.BindTwoWay(vm.SfxEnabled, ref _showDisposables);
            _musicToggle.BindTwoWay(vm.MusicEnabled, ref _showDisposables);
            _closeButton.BindButton(vm.RequestClose, ref _showDisposables);
        }

        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            // UIAnimator sets alpha=1 instantly (null transition). Reset so our fade-in is visible.
            CanvasGroup.alpha = 0f;

            _showSequence = DOTween.Sequence().SetLink(gameObject);
            _showSequence.Join(CanvasGroup.DOFade(1f, 0.18f));
            if (_panel != null)
                _showSequence.Join(_panel.DOScale(1f, 0.35f).SetEase(Ease.OutBack));

            await _showSequence.AwaitAsync(ct);
            _showSequence = null;
        }

        protected override async UniTask OnHideAsync(CancellationToken ct)
        {
            // UIAnimator sets alpha=0 instantly (null transition). Restore so our fade-out is visible.
            CanvasGroup.alpha = 1f;

            _hideSequence = DOTween.Sequence().SetLink(gameObject);
            if (_panel != null)
            {
                _hideSequence.Append(_panel.DOScale(1.05f, 0.06f).SetEase(Ease.OutQuad));
                _hideSequence.Append(_panel.DOScale(0f, 0.18f).SetEase(Ease.InBack));
            }
            _hideSequence.Insert(0f, CanvasGroup.DOFade(0f, 0.22f));

            await _hideSequence.AwaitAsync(ct);
            _hideSequence = null;
        }

        private void OnDisable()
        {
            _showSequence?.Kill(); _showSequence = null;
            _hideSequence?.Kill(); _hideSequence = null;
            CanvasGroup.DOKill();
            if (_panel != null) _panel.DOKill();
        }
    }
}
