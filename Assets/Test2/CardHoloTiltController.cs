using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CardHoloTiltController : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [Header("Tilt")]
    public float MaxTilt = 1f;
    public float SmoothSpeed = 8f;

    private static readonly int TiltXId      = Shader.PropertyToID("_TiltX");
    private static readonly int TiltYId      = Shader.PropertyToID("_TiltY");
    private static readonly int CardAspectId = Shader.PropertyToID("_CardAspect");

    private Image _image;
    private Material _material;
    private RectTransform _rectTransform;
    private Vector2 _targetTilt;
    private float _currentTiltX;
    private float _currentTiltY;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        // Per-card material instance — prevents tilt on one card affecting all others
        _material = Instantiate(_image.material);
        _image.material = _material;

        // Push card aspect ratio so border normals align with screen geometry
        Vector2 size = _rectTransform.rect.size;
        float aspect = size.y > 0f ? size.x / size.y : 0.714f;
        _material.SetFloat(CardAspectId, aspect);
    }

    private void Update()
    {
        _currentTiltX = Mathf.Lerp(_currentTiltX, _targetTilt.x, Time.deltaTime * SmoothSpeed);
        _currentTiltY = Mathf.Lerp(_currentTiltY, _targetTilt.y, Time.deltaTime * SmoothSpeed);
        _material.SetFloat(TiltXId, _currentTiltX);
        _material.SetFloat(TiltYId, _currentTiltY);
    }

    public void OnPointerEnter(PointerEventData eventData) { }

    public void OnPointerExit(PointerEventData eventData)
    {
        _targetTilt = Vector2.zero;
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform, eventData.position, eventData.pressEventCamera, out Vector2 localPoint))
            return;

        Vector2 size = _rectTransform.rect.size;
        Vector2 normalized = new Vector2(
            localPoint.x / (size.x * 0.5f),
            localPoint.y / (size.y * 0.5f)
        );

        _targetTilt = Vector2.ClampMagnitude(normalized * MaxTilt, MaxTilt);
    }

    // Drive tilt from animation or card-flip systems instead of mouse input
    public void SetTilt(Vector2 tilt)
    {
        _targetTilt = Vector2.ClampMagnitude(tilt, MaxTilt);
    }

    private void OnDestroy()
    {
        if (_material != null)
            Destroy(_material);
    }
}
