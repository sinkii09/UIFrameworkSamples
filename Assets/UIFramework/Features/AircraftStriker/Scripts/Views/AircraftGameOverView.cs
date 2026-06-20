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
    [UIViewKey("AircraftStriker/AircraftGameOverView")]
    public class AircraftGameOverView : UIView<AircraftGameOverViewModel>
    {
        public override UILayer Layer => UILayer.Overlay;

        [SerializeField] private TMP_Text   _finalScoreLabel;
        [SerializeField] private TMP_Text   _bestScoreLabel;
        [SerializeField] private TMP_Text   _wavesLabel;
        [SerializeField] private TMP_Text   _coinsLabel;
        [SerializeField] private TMP_Text   _grazeLabel;
        [SerializeField] private TMP_Text   _maxComboLabel;
        [SerializeField] private GameObject _newHighScoreBadge;
        [SerializeField] private Button     _retryButton;
        [SerializeField] private Button     _menuButton;

        [Header("Animation")]
        [SerializeField] private RectTransform _panel;
        [SerializeField] private TMP_Text      _titleText;

        private Sequence _showSequence;
        private Sequence _hideSequence;
        private int      _targetScore;

        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            _hideSequence?.Kill();
            CanvasGroup.alpha = 0f;
            if (_panel != null)     _panel.localScale      = Vector3.zero;
            if (_titleText != null) _titleText.transform.localScale = Vector3.zero;
            _finalScoreLabel.transform.localScale = Vector3.zero;
            _bestScoreLabel.transform.localScale  = Vector3.zero;
            _wavesLabel.transform.localScale      = Vector3.zero;
            _coinsLabel.transform.localScale      = Vector3.zero;
            _grazeLabel.transform.localScale      = Vector3.zero;
            _maxComboLabel.transform.localScale   = Vector3.zero;
            _retryButton.transform.localScale     = Vector3.zero;
            _menuButton.transform.localScale      = Vector3.zero;
        }

        protected override void BindViewModel(AircraftGameOverViewModel vm)
        {
            // Score text is driven by the count-up tween in OnShowAsync; capture target value here.
            _targetScore = vm.FinalScore.Value;
            _finalScoreLabel.SetText("0");

            vm.BestScore.BindToText(_bestScoreLabel, v => $"Best: {v:N0}").AddTo(ref _showDisposables);
            vm.WavesReached.BindToText(_wavesLabel, v => $"Wave {v}").AddTo(ref _showDisposables);
            vm.CoinsEarned.BindToText(_coinsLabel, v => $"+{v} coins").AddTo(ref _showDisposables);
            vm.GrazeCount.BindToText(_grazeLabel, v => $"Graze: {v}").AddTo(ref _showDisposables);
            vm.MaxCombo.BindToText(_maxComboLabel, v => $"Max Combo: x{v}").AddTo(ref _showDisposables);
            vm.IsNewHighScore.BindToActive(_newHighScoreBadge).AddTo(ref _showDisposables);

            _retryButton.BindButton(vm.OnRetryPressed, ref _showDisposables);
            _menuButton.BindButton(vm.OnMenuPressed, ref _showDisposables);
        }

        // Overlay fades in → panel bounces up → stat labels stagger → score counts up.
        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            // UIAnimator sets alpha=1 instantly when no transition; reset so our fade is visible.
            CanvasGroup.alpha = 0f;

            _showSequence = DOTween.Sequence().SetLink(gameObject);
            _showSequence.Join(CanvasGroup.DOFade(1f, 0.2f));

            if (_panel != null)
                _showSequence.Join(_panel.DOScale(1f, 0.4f).SetEase(Ease.OutBack));

            if (_titleText != null)
                _showSequence.Insert(0.2f, _titleText.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

            _showSequence.Insert(0.28f, _wavesLabel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.34f, _coinsLabel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.40f, _grazeLabel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.46f, _maxComboLabel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.52f, _bestScoreLabel.transform.DOScale(1f, 0.25f).SetEase(Ease.OutBack));

            // Score label appears then counts up; buttons appear in parallel so player isn't waiting.
            _showSequence.Insert(0.58f, _finalScoreLabel.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.72f,
                DOVirtual.Float(0, _targetScore, 0.7f, v => _finalScoreLabel.SetText($"{(int)v:N0}"))
                         .SetEase(Ease.OutCubic));

            _showSequence.Insert(0.80f, _retryButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.90f, _menuButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

            await _showSequence.AwaitAsync(ct);
            _showSequence = null;
        }

        protected override async UniTask OnHideAsync(CancellationToken ct)
        {
            CanvasGroup.alpha = 1f;

            _hideSequence = DOTween.Sequence().SetLink(gameObject);
            if (_panel != null)
            {
                _hideSequence.Append(_panel.DOScale(1.06f, 0.07f).SetEase(Ease.OutQuad));
                _hideSequence.Append(_panel.DOScale(0f, 0.22f).SetEase(Ease.InBack));
            }
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
            if (_panel != null)          _panel.DOKill();
            if (_titleText != null)      _titleText.transform.DOKill();
            if (_finalScoreLabel != null) _finalScoreLabel.transform.DOKill();
            if (_bestScoreLabel  != null) _bestScoreLabel.transform.DOKill();
            if (_wavesLabel      != null) _wavesLabel.transform.DOKill();
            if (_coinsLabel      != null) _coinsLabel.transform.DOKill();
            if (_grazeLabel      != null) _grazeLabel.transform.DOKill();
            if (_maxComboLabel   != null) _maxComboLabel.transform.DOKill();
            if (_retryButton     != null) _retryButton.transform.DOKill();
            if (_menuButton      != null) _menuButton.transform.DOKill();
        }
    }
}
