using UnityEngine;
using UnityEngine.EventSystems;

namespace AircraftStriker
{
    // Full-screen transparent canvas panel. Registered via RegisterInstance in AircraftLifetimeScope.
    // Script Execution Order: LateUpdate must run AFTER PlayerController.Update.
    [RequireComponent(typeof(RectTransform))]
    public class AircraftInputHandler : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        private Camera _camera;
        private Vector2 _lastScreenPos;

        public bool IsDragging { get; private set; }
        public Vector2 WorldDeltaThisFrame { get; private set; }

        private void Awake() => _camera = Camera.main;

        private void LateUpdate() => WorldDeltaThisFrame = Vector2.zero;

        public void OnPointerDown(PointerEventData e)
        {
            IsDragging = true;
            _lastScreenPos = e.position;
        }

        public void OnDrag(PointerEventData e)
        {
            Vector2 screenDelta = e.position - _lastScreenPos;
            _lastScreenPos = e.position;
            float worldUnitsPerPixel = _camera.orthographicSize * 2f / Screen.height;
            WorldDeltaThisFrame = screenDelta * worldUnitsPerPixel;
        }

        public void OnPointerUp(PointerEventData e)
        {
            IsDragging = false;
            WorldDeltaThisFrame = Vector2.zero;
        }
    }
}
