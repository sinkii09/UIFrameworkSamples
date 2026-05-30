using System.Collections;
using UnityEngine;

public class BurnDissolveController : MonoBehaviour
{
    [Header("Animation")]
    public float Duration = 2f;
    public bool Loop = false;
    public float LoopDelay = 0.5f;
    public bool AutoPlay = true;
    public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private static readonly int DissolveAmountID = Shader.PropertyToID("_DisolveAmount");

    private Material _material;
    private Coroutine _routine;

    private void Awake()
    {
        var sr = GetComponent<SpriteRenderer>();
        _material = sr != null ? sr.material : GetComponent<Renderer>()?.material;
    }

    private void Start()
    {
        if (AutoPlay) Play();
    }

    public void Play()
    {
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(RunDissolve());
    }

    public void Stop()
    {
        if (_routine == null) return;
        StopCoroutine(_routine);
        _routine = null;
    }

    public void ResetToFull()
    {
        Stop();
        SetDissolve(0f);
    }

    private void SetDissolve(float value)
    {
        _material?.SetFloat(DissolveAmountID, value);
    }

    private IEnumerator RunDissolve()
    {
        do
        {
            SetDissolve(0f);

            float elapsed = 0f;
            while (elapsed < Duration)
            {
                SetDissolve(Curve.Evaluate(elapsed / Duration));
                elapsed += Time.deltaTime;
                yield return null;
            }

            SetDissolve(Curve.Evaluate(1f));

            if (Loop)
            {
                if (LoopDelay > 0f)
                    yield return new WaitForSeconds(LoopDelay);
            }
        }
        while (Loop);

        _routine = null;
    }

    private void OnDestroy()
    {
        // Destroy the instanced material created by .material accessor
        if (_material != null)
            Destroy(_material);
    }
}
