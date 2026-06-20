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
    [UIViewKey("AircraftStriker/AircraftVictoryView")]
    public class AircraftVictoryView : UIView<AircraftVictoryViewModel>
    {
        public override UILayer Layer => UILayer.Overlay;

        [SerializeField] private TMP_Text   _finalScoreLabel;
        [SerializeField] private TMP_Text   _bestScoreLabel;
        [SerializeField] private TMP_Text   _coinsLabel;
        [SerializeField] private GameObject _newHighScoreBadge;
        [SerializeField] private Button     _playAgainButton;
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
            if (_panel != null)     _panel.localScale             = Vector3.zero;
            if (_titleText != null) _titleText.transform.localScale = Vector3.zero;
            _finalScoreLabel.transform.localScale  = Vector3.zero;
            _bestScoreLabel.transform.localScale   = Vector3.zero;
            _coinsLabel.transform.localScale       = Vector3.zero;
            _playAgainButton.transform.localScale  = Vector3.zero;
            _menuButton.transform.localScale       = Vector3.zero;
        }

        protected override void BindViewModel(AircraftVictoryViewModel vm)
        {
            _targetScore = vm.FinalScore.Value;
            _finalScoreLabel.SetText("0");

            vm.BestScore.BindToText(_bestScoreLabel, v => $"Best: {v:N0}").AddTo(ref _showDisposables);
            vm.CoinsEarned.BindToText(_coinsLabel, v => $"+{v} coins").AddTo(ref _showDisposables);
            vm.IsNewHighScore.BindToActive(_newHighScoreBadge).AddTo(ref _showDisposables);

            _playAgainButton.BindButton(vm.OnPlayAgainPressed, ref _showDisposables);
            _menuButton.BindButton(vm.OnMenuPressed, ref _showDisposables);
        }

        // Celebration: overlay fades in → panel bounces in with extra overshooting back ease →
        // title scales up with big OutBack → stats stagger in → score counts up.
        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            CanvasGroup.alpha = 0f;

            _showSequence = DOTween.Sequence().SetLink(gameObject);
            _showSequence.Join(CanvasGroup.DOFade(1f, 0.2f));

            if (_panel != null)
                _showSequence.Join(_panel.DOScale(1f, 0.5f).SetEase(Ease.OutBack, 1.8f));

            if (_titleText != null)
                _showSequence.Insert(0.2f, _titleText.transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack, 2f));

            _showSequence.Insert(0.38f, _coinsLabel.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.46f, _bestScoreLabel.transform.DOScale(1f, 0.28f).SetEase(Ease.OutBack));
            _showSequence.Insert(0.54f, _finalScoreLabel.transform.DOScale(1f, 0.32f).SetEase(Ease.OutBack));

            // Score count-up starts after the label appears.
            _showSequence.Insert(0.72f,
                DOVirtual.Float(0, _targetScore, 1.2f, v => _finalScoreLabel.SetText($"{(int)v:N0}"))
                         .SetEase(Ease.OutCubic));

            _showSequence.Insert(2.0f, _playAgainButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));
            _showSequence.Insert(2.1f, _menuButton.transform.DOScale(1f, 0.3f).SetEase(Ease.OutBack));

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
            if (_panel           != null) _panel.DOKill();
            if (_titleText       != null) _titleText.transform.DOKill();
            if (_finalScoreLabel != null) _finalScoreLabel.transform.DOKill();
            if (_bestScoreLabel  != null) _bestScoreLabel.transform.DOKill();
            if (_coinsLabel      != null) _coinsLabel.transform.DOKill();
            if (_playAgainButton != null) _playAgainButton.transform.DOKill();
            if (_menuButton      != null) _menuButton.transform.DOKill();
        }
    }
}
