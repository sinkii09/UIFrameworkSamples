using UnityEngine;
using UnityEngine.UI;

/// Automatically feeds the texture from a SpriteRenderer or UI Image
/// into the _CardTexture property of the Edition-Polychrome material.
public class PolychromeTextureSync : MonoBehaviour
{
    [Tooltip("Leave empty to auto-detect from SpriteRenderer or Image on this GameObject.")]
    [SerializeField] private Texture2D _overrideTexture;

    private MaterialPropertyBlock _block;
    private Renderer _renderer;

    private static readonly int CardTextureId = Shader.PropertyToID("_CardTexture");

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _block = new MaterialPropertyBlock();
    }

    private void Start()
    {
        Apply();
    }

    public void Apply()
    {
        Texture2D tex = _overrideTexture != null ? _overrideTexture : ResolveTexture();
        if (tex == null)
        {
            Debug.LogWarning($"[PolychromeTextureSync] No texture found on {name}", this);
            return;
        }

        _renderer.GetPropertyBlock(_block);
        _block.SetTexture(CardTextureId, tex);
        _renderer.SetPropertyBlock(_block);
    }

    private Texture2D ResolveTexture()
    {
        // SpriteRenderer on same GameObject
        if (TryGetComponent(out SpriteRenderer sr) && sr.sprite != null)
            return sr.sprite.texture;

        // UI Image on same GameObject
        if (TryGetComponent(out Image img) && img.sprite != null)
            return img.sprite.texture;

        return null;
    }
}
