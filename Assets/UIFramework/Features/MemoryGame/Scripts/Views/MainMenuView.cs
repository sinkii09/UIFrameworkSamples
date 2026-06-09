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
    public class MainMenuView : UIView<MainMenuViewModel>
    {
        [SerializeField] private TMP_Text _titleText;
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        [Header("Juice")]
        [SerializeField] private float _showDuration = 0.45f;
        [SerializeField] private float _buttonStagger = 0.08f;

        public override UILayer Layer => UILayer.Screen;

        // Buttons live inside a LayoutGroup — position animation (DOAnchorPosY) is overridden
        // by the group every frame. Scale animation is unaffected by layout, so we use that instead.
        private Transform[] _buttonTransforms;
        private Sequence _showSequence;

        protected override void Awake()
        {
            base.Awake();
            _buttonTransforms = new Transform[]
            {
                _playButton.transform,
                _settingsButton.transform,
                _quitButton.transform
            };
        }

        // Called by UIViewBase.ShowAsync before SetActive(true) — resets visual state so
        // the first rendered frame after activation is already at the animation start position.
        protected override void OnPrepareForShow()
        {
            _showSequence?.Kill();
            _showSequence = null;
            // ZoomOutFadeTransition leaves root at ZoomOutScale after hide — reset to 1 for next show.
            transform.localScale = Vector3.one;
            _titleText.transform.localScale = Vector3.zero;
            foreach (var t in _buttonTransforms)
                t.localScale = Vector3.zero;
        }

        protected override void BindViewModel(MainMenuViewModel vm)
        {
            vm.Title.BindToText(_titleText).AddTo(ref _showDisposables);
            _playButton.BindButton(vm.RequestPlay, ref _showDisposables);
            _settingsButton.BindButton(vm.RequestSettings, ref _showDisposables);
            _quitButton.BindButton(vm.RequestQuit, ref _showDisposables);
        }

        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            _showSequence = DOTween.Sequence();
            _showSequence.Join(_titleText.transform
                .DOScale(1f, _showDuration)
                .SetEase(Ease.OutBack));

            for (int i = 0; i < _buttonTransforms.Length; i++)
            {
                _showSequence.Insert(0.15f + i * _buttonStagger,
                    _buttonTransforms[i].DOScale(1f, _showDuration)
                                        .SetEase(Ease.OutBack));
            }

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
            transform.DOKill();
            if (_titleText != null) _titleText.transform.DOKill();
            if (_buttonTransforms != null)
                foreach (var t in _buttonTransforms)
                    t?.DOKill();
        }

    }
}
