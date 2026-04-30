using DG.Tweening;
using UnityEngine;

/// <summary>
/// Squash & stretch + anticipation tween untuk player visual. Subscribe ke
/// game events relevant (dash, throw, catch, charge release) dan kasih
/// scale-punch / squash response.
/// </summary>
public class PlayerJuice : MonoBehaviour
{
    [Header("Squash & Stretch")]
    [SerializeField] private float dashSquashAmount  = 0.25f;
    [SerializeField] private float dashSquashTime    = 0.12f;
    [SerializeField] private float catchPunchAmount  = 0.2f;
    [SerializeField] private float catchPunchTime    = 0.25f;
    [SerializeField] private float throwStretchAmount= 0.18f;
    [SerializeField] private float throwStretchTime  = 0.18f;
    [SerializeField] private float chargeReleasePunch= 0.15f;

    private PlayerController _player;
    private Transform        _target;
    private Vector3          _baseScale;
    private Tween            _activeTween;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
        _target = _player != null && _player.visual != null
            ? _player.visual.transform
            : transform;
        _baseScale = _target.localScale;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDashed     += HandleDash;
        GameEvents.OnBoomerangCaught  += HandleCatch;
        GameEvents.OnBoomerangThrown  += HandleThrow;
        GameEvents.OnChargeReleased   += HandleChargeRelease;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDashed     -= HandleDash;
        GameEvents.OnBoomerangCaught  -= HandleCatch;
        GameEvents.OnBoomerangThrown  -= HandleThrow;
        GameEvents.OnChargeReleased   -= HandleChargeRelease;
    }

    private bool IsMine(int playerIndex)
        => _player == null || _player.PlayerIndex == playerIndex;

    private void HandleDash(int idx)
    {
        if (!IsMine(idx)) return;
#if UNITY_EDITOR
        Debug.Log($"[Juice {idx}] dash squash");
#endif
        // Squash: lebar pendek, naik tinggi
        Squash(new Vector3(1f + dashSquashAmount, 1f - dashSquashAmount, 1f + dashSquashAmount), dashSquashTime);
    }

    private void HandleCatch(int idx)
    {
        if (!IsMine(idx)) return;
#if UNITY_EDITOR
        Debug.Log($"[Juice {idx}] catch punch");
#endif
        Punch(catchPunchAmount, catchPunchTime);
    }

    private void HandleThrow(int idx)
    {
        if (!IsMine(idx)) return;
#if UNITY_EDITOR
        Debug.Log($"[Juice {idx}] throw stretch");
#endif
        // Stretch ke arah depan
        Squash(new Vector3(1f - throwStretchAmount, 1f + throwStretchAmount, 1f + throwStretchAmount), throwStretchTime);
    }

    private void HandleChargeRelease(int idx, float charge01)
    {
        if (!IsMine(idx)) return;
#if UNITY_EDITOR
        Debug.Log($"[Juice {idx}] charge release punch charge={charge01:F2}");
#endif
        Punch(chargeReleasePunch * (0.5f + 0.5f * charge01), throwStretchTime);
    }

    private void Squash(Vector3 squashScale, float duration)
    {
        _activeTween?.Kill();
        _target.localScale = Vector3.Scale(_baseScale, squashScale);
        _activeTween = _target.DOScale(_baseScale, duration)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }

    private void Punch(float amount, float duration)
    {
        _activeTween?.Kill();
        _target.localScale = _baseScale;
        _activeTween = _target.DOPunchScale(Vector3.one * amount, duration, 6, 0.6f)
            .SetUpdate(true);
    }
}
