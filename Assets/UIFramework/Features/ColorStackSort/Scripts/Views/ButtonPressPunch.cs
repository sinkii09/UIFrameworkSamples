using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ColorStackSort
{
    /// <summary>
    /// Scale punch on press, for buttons whose <c>Transition</c> is set to None. Drop it next to a
    /// <see cref="Selectable"/>; it drives that object's own rect unless given another target.
    /// </summary>
    /// <remarks>
    /// Punches <c>localScale</c>, never <c>anchoredPosition</c>. Scale is safe under any layout
    /// strategy, including a LayoutGroup, which rewrites position every frame and would erase a
    /// positional tween outright.
    /// </remarks>
    [RequireComponent(typeof(RectTransform))]
    public sealed class ButtonPressPunch : MonoBehaviour, IPointerDownHandler
    {
        private static readonly Vector3 PressPunch = new Vector3(-0.12f, -0.12f, 0f);
        private const float PunchDuration = 0.18f;
        private const int PunchVibrato = 6;
        private const float PunchElasticity = 0.6f;

        [SerializeField] private RectTransform _target;
        [SerializeField] private Selectable _selectable;

        /// <summary>
        /// The authored scale, captured once. Restoring a hardcoded <c>Vector3.one</c> instead would
        /// silently normalise any button authored at a different scale on its first press — the same
        /// trap <see cref="TubeFeedback"/> avoids by caching its base colour rather than assuming it.
        /// </summary>
        private Vector3 _baseScale = Vector3.one;

        private void Awake()
        {
            if (_target == null) _target = (RectTransform)transform;
            if (_selectable == null) _selectable = GetComponent<Selectable>();
            if (_target != null) _baseScale = _target.localScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Selectable.interactable does NOT stop the EventSystem delivering pointer events to
            // other handlers on the same object — only the Selectable itself opts out. Without this
            // check a greyed-out Undo would still animate as though it had done something.
            if (_selectable != null && !_selectable.IsInteractable()) return;
            if (_target == null) return;

            Restore();
            _target.DOPunchScale(PressPunch, PunchDuration, PunchVibrato, PunchElasticity)
                .SetLink(gameObject);
        }

        // SetLink kills on destroy, not on disable, so a punch would otherwise resume mid-squash
        // when the panel is shown again — and a punch killed partway never restores its own scale.
        private void OnDisable() => Restore();

        private void Restore()
        {
            if (_target == null) return;

            _target.DOKill();
            _target.localScale = _baseScale;
        }
    }
}
