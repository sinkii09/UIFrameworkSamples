using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AircraftStriker
{
    [UIViewKey("AircraftStriker/AircraftMainMenuView")]
    public class AircraftMainMenuView : UIView<AircraftMainMenuViewModel>
    {
        public override UILayer Layer => UILayer.Screen;

        [SerializeField] private TMP_Text _bestScoreLabel;
        [SerializeField] private Button   _playButton;
        [SerializeField] private Button   _shopButton;

        [Header("Animation")]
        [SerializeField] private RectTransform _titleTransform;
        [SerializeField] private float         _showDuration   = 0.45f;
        [SerializeField] private float         _buttonStagger  = 0.08f;

        private Sequence _showSequence;

        // Buttons live inside a LayoutGroup — use DOScale, not DOAnchorPosY.
        // Reset to scale 0 before the first rendered frame after activation.
        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            _showSequence = null;
            if (_titleTransform != null) _titleTransform.localScale = Vector3.zero;
            _bestScoreLabel.transform.localScale = Vector3.zero;
            _playButton.transform.localScale     = Vector3.zero;
            _shopButton.transform.localScale     = Vector3.zero;
        }

        protected override void BindViewModel(AircraftMainMenuViewModel vm)
        {
            vm.BestScore.BindToText(_bestScoreLabel, v => $"Best: {v:N0}").AddTo(ref _showDisposables);
            _playButton.BindButton(vm.OnPlayPressed, ref _showDisposables);
            _shopButton.BindButton(vm.OnShopPressed, ref _showDisposables);
        }

        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            _showSequence = DOTween.Sequence().SetLink(gameObject);

            if (_titleTransform != null)
                _showSequence.Append(_titleTransform.DOScale(1f, _showDuration).SetEase(Ease.OutBack));

            _showSequence.Insert(0.15f,                        _bestScoreLabel.transform.DOScale(1f, _showDuration).SetEase(Ease.OutBack));
            _showSequence.Insert(0.15f + _buttonStagger,       _playButton.transform.DOScale(1f, _showDuration).SetEase(Ease.OutBack));
            _showSequence.Insert(0.15f + _buttonStagger * 2f,  _shopButton.transform.DOScale(1f, _showDuration).SetEase(Ease.OutBack));

            await _showSequence.AwaitAsync(ct);
            _showSequence = null;
        }

        protected override UniTask OnHideAsync(CancellationToken ct)
        {
            _showSequence?.Kill();
            _showSequence = null;
            return UniTask.CompletedTask;
        }

        private void OnDisable()
        {
            _showSequence?.Kill();
            _showSequence = null;
            if (_titleTransform != null)     _titleTransform.DOKill();
            if (_bestScoreLabel != null)     _bestScoreLabel.transform.DOKill();
            if (_playButton     != null)     _playButton.transform.DOKill();
            if (_shopButton     != null)     _shopButton.transform.DOKill();
        }
    }
}
