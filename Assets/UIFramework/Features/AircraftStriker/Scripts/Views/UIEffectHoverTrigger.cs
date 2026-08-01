using Coffee.UIEffects;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace AircraftStriker
{
    public class UIEffectHoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private UIEffectTweener _tweener;
        [SerializeField] private float _hoverScale    = 1.08f;
        [SerializeField] private float _scaleDuration  = 0.15f;

        private Tween _scaleTween;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _tweener.PlayForward(true);

            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(_hoverScale, _scaleDuration).SetEase(Ease.OutBack).SetLink(gameObject);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _scaleTween?.Kill();
            _scaleTween = transform.DOScale(1f, _scaleDuration).SetEase(Ease.OutQuad).SetLink(gameObject);
        }

        private void OnDisable()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }
    }
}
