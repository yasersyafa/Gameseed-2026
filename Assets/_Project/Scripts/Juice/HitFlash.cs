using System.Collections;
using UnityEngine;

/// <summary>
/// Material flash burst saat hit. Pakai MaterialPropertyBlock biar tidak
/// instantiate material baru. Subscribe ke GameEvents.OnPlayerHit jika
/// gameObject punya PlayerController dengan PlayerIndex match.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [Header("Flash")]
    [SerializeField] private Color flashColor   = Color.white;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private float flashIntensity = 4f;

    [Header("Auto-subscribe (optional)")]
    [SerializeField] private bool subscribePlayerHit = true;

    private Renderer[] _renderers;
    private MaterialPropertyBlock _block;
    private static readonly int  BaseColorId      = Shader.PropertyToID("_BaseColor");
    private static readonly int  EmissionColorId  = Shader.PropertyToID("_EmissionColor");
    private Color[] _originalColors;
    private PlayerController _player;
    private Coroutine _flashRoutine;

    private void Awake()
    {
        _renderers = GetComponentsInChildren<Renderer>();
        _block     = new MaterialPropertyBlock();
        _player    = GetComponent<PlayerController>();

        _originalColors = new Color[_renderers.Length];
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_block);
            _originalColors[i] = _renderers[i].sharedMaterial != null
                ? _renderers[i].sharedMaterial.color
                : Color.white;
        }
    }

    private void OnEnable()
    {
        if (subscribePlayerHit)
            GameEvents.OnPlayerHit += HandlePlayerHit;
    }

    private void OnDisable()
    {
        if (subscribePlayerHit)
            GameEvents.OnPlayerHit -= HandlePlayerHit;
    }

    private void HandlePlayerHit(int index, PlayerController controller)
    {
        if (_player != null && _player.PlayerIndex != index) return;
        Flash();
    }

    public void Flash() => Flash(flashColor, flashDuration);

    public void Flash(Color color, float duration)
    {
        if (_renderers == null || _renderers.Length == 0) return;

#if UNITY_EDITOR
        Debug.Log($"[HitFlash] Flash on {gameObject.name} color={color} dur={duration:F2}");
#endif

        if (_flashRoutine != null) StopCoroutine(_flashRoutine);
        _flashRoutine = StartCoroutine(FlashRoutine(color, duration));
    }

    private IEnumerator FlashRoutine(Color color, float duration)
    {
        ApplyColor(color, color * flashIntensity);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        ResetColors();
        _flashRoutine = null;
    }

    private void ApplyColor(Color baseCol, Color emission)
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, baseCol);
            _block.SetColor(EmissionColorId, emission);
            _renderers[i].SetPropertyBlock(_block);
        }
    }

    private void ResetColors()
    {
        for (int i = 0; i < _renderers.Length; i++)
        {
            _renderers[i].GetPropertyBlock(_block);
            _block.SetColor(BaseColorId, _originalColors[i]);
            _block.SetColor(EmissionColorId, Color.black);
            _renderers[i].SetPropertyBlock(_block);
        }
    }
}
