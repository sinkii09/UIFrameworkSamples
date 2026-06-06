using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MemoryGame
{
    [RequireComponent(typeof(Button))]
    public class CardView : MonoBehaviour
    {
        [SerializeField] private Image _frontImage;
        [SerializeField] private Image _backImage;
        [SerializeField] private Button _button;

        private const float HalfFlipDuration = 0.12f;

        public int CardId { get; private set; }
        public Action<int> OnClicked;

        private bool _isAnimating;
        private bool _isFaceUp;

        private void Awake()
        {
            _button.onClick.AddListener(() =>
            {
                if (!_isAnimating) OnClicked?.Invoke(CardId);
            });
        }

        public void Setup(int cardId, Sprite faceSprite, Sprite backSprite = null)
        {
            CardId = cardId;
            _frontImage.sprite = faceSprite;
            if (backSprite != null) _backImage.sprite = backSprite;
            _frontImage.gameObject.SetActive(false);
            _backImage.gameObject.SetActive(true);
            _isFaceUp = false;
            _isAnimating = false;
            _button.interactable = true;
        }

        public void FlipToFront(Action onComplete = null)
        {
            if (_isFaceUp) { onComplete?.Invoke(); return; }
            AnimateFlip(toFront: true, onComplete);
        }

        public void FlipToBack(Action onComplete = null)
        {
            if (!_isFaceUp) { onComplete?.Invoke(); return; }
            AnimateFlip(toFront: false, onComplete);
        }

        public void PlayMatchEffect()
        {
            _button.interactable = false;
            // DOKill stops any in-progress flip tween; _isAnimating may be stale after kill
            // but is harmless — card is destroyed at session end and button is already disabled.
            transform.DOKill();
            // Punch → burst scale-up → implode; _locked in MemoryCardGame prevents mismatch
            // flip-back from racing with this sequence.
            DOTween.Sequence()
                .Append(transform.DOPunchScale(Vector3.one * 0.25f, 0.35f, 6, 0.5f))
                .Append(transform.DOScale(1.2f, 0.1f))
                // Scale to zero instead of SetActive(false) — keeps the grid slot reserved so
                // GridLayoutGroup does not recalculate and shift remaining cards.
                .Append(transform.DOScale(0f, 0.25f).SetEase(Ease.InBack));
        }

        private void AnimateFlip(bool toFront, Action onComplete)
        {
            // Kill any running scale tween before starting a new one to prevent layered animations.
            transform.DOKill();
            _isAnimating = true;
            transform.DOScaleX(0f, HalfFlipDuration)
                .SetEase(Ease.InSine)
                .OnComplete(() =>
                {
                    _backImage.gameObject.SetActive(!toFront);
                    _frontImage.gameObject.SetActive(toFront);
                    transform.DOScaleX(1f, HalfFlipDuration)
                        .SetEase(Ease.OutSine)
                        .OnComplete(() =>
                        {
                            _isFaceUp = toFront;
                            _isAnimating = false;
                            onComplete?.Invoke();
                        });
                });
        }

        private void OnDestroy() => transform.DOKill();
    }
}
