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
        [SerializeField] private float _hideDuration = 0.2f;
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

        protected override void BindViewModel(MainMenuViewModel vm)
        {
            vm.Title.BindToText(_titleText).AddTo(ref _showDisposables);
            _playButton.BindButton(vm.RequestPlay, ref _showDisposables);
            _settingsButton.BindButton(vm.RequestSettings, ref _showDisposables);
            _quitButton.BindButton(vm.RequestQuit, ref _showDisposables);
        }

        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            // Unconditional reset — covers cancellation-mid-hide leaving stale scale.
            _showSequence?.Kill();
            _titleText.transform.localScale = Vector3.zero;
            foreach (var t in _buttonTransforms)
                t.localScale = Vector3.zero;

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

            await AwaitTween(_showSequence, ct);
            _showSequence = null;
        }

        protected override async UniTask OnHideAsync(CancellationToken ct)
        {
            _showSequence?.Kill();
            _showSequence = null;

            // Hide sequence is intentionally a local — OnDisable.DOKill() is the backstop
            // if the object is force-deactivated mid-hide.
            var seq = DOTween.Sequence();
            seq.Join(_titleText.transform.DOScale(0f, _hideDuration).SetEase(Ease.InBack));
            foreach (var t in _buttonTransforms)
                seq.Join(t.DOScale(0f, _hideDuration).SetEase(Ease.InBack));

            try
            {
                await AwaitTween(seq, ct);
            }
            finally
            {
                // Restore start-state for the next OnShowAsync regardless of cancellation.
                _titleText.transform.localScale = Vector3.zero;
                foreach (var t in _buttonTransforms)
                    t.localScale = Vector3.one;
            }
        }

        private void OnDisable()
        {
            _showSequence?.Kill();
            _showSequence = null;
            if (_titleText != null) _titleText.transform.DOKill();
            if (_buttonTransforms != null)
                foreach (var t in _buttonTransforms)
                    t?.DOKill();
        }

        // Bridges DOTween Tween to UniTask without UNITASK_DOTWEEN_SUPPORT.
        // Mirrors DOTweenUIAnimator.AwaitTween — SetUpdate(true) keeps it running at timeScale=0.
        // Registration disposed on complete/kill prevents stale callbacks on DOTween's pooled tweens.
        private static UniTask AwaitTween(Tween tween, CancellationToken ct)
        {
            tween.SetUpdate(true);
            var tcs = new UniTaskCompletionSource();
            CancellationTokenRegistration reg = default;
            tween.OnComplete(() => { reg.Dispose(); tcs.TrySetResult(); })
                 .OnKill(() => { reg.Dispose(); tcs.TrySetCanceled(); });
            if (ct.CanBeCanceled)
                reg = ct.Register(() => tween.Kill());
            return tcs.Task;
        }
    }
}
