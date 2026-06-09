using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using R3;
using Sinkii09.UIFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

namespace MemoryGame
{
    public class GameplayView : UIView<GameplayViewModel>
    {
        [SerializeField] private CardView _cardPrefab;
        [SerializeField] private Transform _gridParent;
        [SerializeField] private TMP_Text _movesText;
        [SerializeField] private TMP_Text _matchesText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private Button _menuButton;
        // 8 sprites — assign in Inspector (e.g. Feel CardsUI art or custom sprites)
        [SerializeField] private Sprite[] _cardFaceSprites;
        [SerializeField] private Sprite _cardBackSprite;

        public override UILayer Layer => UILayer.Screen;

        // Injected by VContainer's InjectGameObject call in UIViewFactory — not via BindViewModel.
        [Inject] private IUINavigator _navigator;

        private readonly List<CardView> _cardViews = new();
        private CancellationTokenSource _winViewCts;
        private bool _navigatingToMenu;

        // Called by UIViewBase.ShowAsync before SetActive(true) — resets root scale so
        // the first rendered frame after activation is already at the punch-in starting scale.
        protected override void OnPrepareForShow()
        {
            transform.localScale = Vector3.zero;
        }

        // BindViewModel is called once at init, before ShowAsync (before vm.OnShow).
        // Only set up subscriptions here — _game is null until vm.OnShow() runs.
        protected override void BindViewModel(GameplayViewModel vm)
        {
            vm.Moves.Subscribe(v => _movesText.SetText($"Moves: {v}"))
                .AddTo(ref _showDisposables);
            vm.MatchesFound.Subscribe(v => _matchesText.SetText($"Matches: {v}/8"))
                .AddTo(ref _showDisposables);
            vm.TimerText.Subscribe(v => _timerText.SetText(v))
                .AddTo(ref _showDisposables);

            vm.FlipToFront.Subscribe(id => GetCard(id)?.FlipToFront())
                .AddTo(ref _showDisposables);
            vm.FlipToBack.Subscribe(pair =>
            {
                GetCard(pair.Id1)?.FlipToBack();
                GetCard(pair.Id2)?.FlipToBack();
            }).AddTo(ref _showDisposables);
            vm.MatchConfirmed.Subscribe(pair =>
            {
                GetCard(pair.Id1)?.PlayMatchEffect();
                GetCard(pair.Id2)?.PlayMatchEffect();
            }).AddTo(ref _showDisposables);

            // vm.OnShow() has already run by the time GameWon fires — navigator is valid.
            vm.GameWon.Subscribe(args =>
            {
                ShowWinViewDelayedAsync(args).Forget();
            }).AddTo(ref _showDisposables);

            if (_menuButton != null)
                _menuButton.OnClickAsObservable()
                    .Subscribe(_ => GoToMainMenuAsync().Forget())
                    .AddTo(ref _showDisposables);
        }

        // 1000 ms = card disappear animation (~700 ms) + 300 ms pause before win screen.
        private async UniTaskVoid ShowWinViewDelayedAsync(WinArgs args)
        {
            _winViewCts?.Cancel();
            _winViewCts?.Dispose();
            _winViewCts = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            try
            {
                await UniTask.Delay(1000, cancellationToken: _winViewCts.Token);
                await _navigator.ShowAsync<WinView, WinArgs>(args, _winViewCts.Token);
            }
            catch (OperationCanceledException) { /* cancelled by GoToMainMenuAsync or destroy — intentional */ }
            finally
            {
                _winViewCts?.Dispose();
                _winViewCts = null;
            }
        }

        private async UniTaskVoid GoToMainMenuAsync()
        {
            if (_navigatingToMenu) return;
            _navigatingToMenu = true;
            // Cancel any pending/in-flight WinView show. Without this, ShowAsync<WinView> could
            // re-acquire _isTransitioning in the one-frame gap between WaitUntil and CloseAllAsync,
            // causing CloseAllAsync to be silently dropped (same class of bug being fixed here).
            _winViewCts?.Cancel();
            try
            {
                // Wait for any in-flight transition (entrance anim, cancelled WinView show, etc.)
                // before navigating — CloseAllAsync is silently dropped when _isTransitioning is true.
                // Safe: UINavigator always clears _isTransitioning in its finally blocks, so this
                // cannot deadlock. Assumes UIViewBase.HideAsync deactivates (SetActive false) rather
                // than Destroys the GO — if that changes, destroyCancellationToken may fire mid-
                // CloseAllAsync and leave ResetState/ShowAsync unexecuted.
                await UniTask.WaitUntil(() => !_navigator.IsTransitioning,
                    cancellationToken: destroyCancellationToken);
                await _navigator.CloseAllAsync();
                _navigator.ResetState();
                await _navigator.ShowAsync<MainMenuView>();
            }
            catch (OperationCanceledException) { /* view destroyed mid-navigation — intentional */ }
            catch (Exception ex) { Debug.LogError($"[GameplayView] GoToMainMenuAsync failed: {ex}"); }
            finally { _navigatingToMenu = false; }
        }

        // Called after vm.OnShow() — _game is initialized so GetInitialCards() is safe.
        protected override async UniTask OnShowAsync(CancellationToken ct)
        {
            SpawnGrid(ViewModel);
            // Hide cards at zero-scale so they're invisible during the screen entrance punch.
            // Parent is already fading in via CanvasGroup — no flash occurs.
            foreach (var card in _cardViews)
                card.transform.localScale = Vector3.zero;

            try
            {
                await transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack).AwaitAsync(ct);

                // Stagger cards in after the screen has settled.
                // SetLink ensures the sequence is killed if the GO is disabled mid-spawn.
                var seq = DOTween.Sequence().SetLink(gameObject);
                for (int i = 0; i < _cardViews.Count; i++)
                    seq.Insert(i * 0.05f, _cardViews[i].transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack));
                await seq.AwaitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                DestroyCards();
                throw;
            }
        }

        protected override UniTask OnHideAsync(CancellationToken ct)
        {
            DestroyCards();
            return UniTask.CompletedTask;
        }

        private void OnDisable()
        {
            transform.DOKill();
            _winViewCts?.Cancel();
            _winViewCts?.Dispose();
            _winViewCts = null;
        }

        private void SpawnGrid(GameplayViewModel vm)
        {
            DestroyCards();
            if (_cardFaceSprites == null || _cardFaceSprites.Length < 8)
            {
                Debug.LogWarning("[GameplayView] _cardFaceSprites needs at least 8 sprites. " +
                                 "Assign them in the Inspector on the GameplayView prefab.");
            }

            var cards = vm.GetInitialCards();
            foreach (var data in cards)
            {
                var cardView = Instantiate(_cardPrefab, _gridParent);
                int faceIndex = data.FaceIndex;
                var sprite = (_cardFaceSprites != null && faceIndex < _cardFaceSprites.Length)
                    ? _cardFaceSprites[faceIndex]
                    : null;
                cardView.Setup(data.Id, sprite, _cardBackSprite);
                cardView.OnClicked += id => vm.HandleCardClick(id);
                _cardViews.Add(cardView);
            }
        }

        private void DestroyCards()
        {
            foreach (var card in _cardViews)
                if (card != null) Destroy(card.gameObject);
            _cardViews.Clear();
        }

        private CardView GetCard(int id) =>
            id >= 0 && id < _cardViews.Count ? _cardViews[id] : null;
    }
}
