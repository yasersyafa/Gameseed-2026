using UnityEngine;

/// <summary>
/// Per-player ground ring tinted by the top-of-stack active power-up. Gives
/// opponents a readable cue for which buff(s) the player is carrying — fills
/// the role the deleted HUD power-up row used to play. Also flashes the
/// player visual on `OnPowerUpRemoved` so the holder feels expiry / shield
/// consume.
///
/// Auto-added by <see cref="PlayerController.EnsureRuntimeComponents"/>.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PowerUpAuraVfx : MonoBehaviour
{
    [SerializeField] private float ringScale     = 1.4f;
    [SerializeField] private float ringHeight    = 0.04f;
    [SerializeField] private float pulseHz       = 1.5f;
    [SerializeField] private float pulseAmp      = 0.12f;
    [SerializeField] private float alpha         = 0.55f;
    [SerializeField] private float removeFlashTime = 0.18f;

    private static Material _sharedMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private PlayerController _player;
    private GameObject       _ring;
    private Color            _ringColor = Color.white;
    private float            _spawnTime;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        GameEvents.OnPickupCollected += HandlePickupCollected;
        GameEvents.OnPowerUpRemoved  += HandlePowerUpRemoved;
    }

    private void OnDisable()
    {
        GameEvents.OnPickupCollected -= HandlePickupCollected;
        GameEvents.OnPowerUpRemoved  -= HandlePowerUpRemoved;
        ReleaseRing();
    }

    private bool IsMine(int idx) => _player != null && _player.PlayerIndex == idx;

    // ── Event handlers ──────────────────────────────────────────────────────
    private void HandlePickupCollected(int idx, int key, Vector3 worldPos)
    {
        if (!IsMine(idx)) return;
        RefreshFromStack();
    }

    private void HandlePowerUpRemoved(int idx, int key)
    {
        if (!IsMine(idx)) return;

        // Brief tint flash so the holder notices the loss.
        var hf = _player != null ? _player.GetComponent<HitFlash>() : null;
        if (hf != null) hf.Flash(PowerUpVisuals.GetTint(key), removeFlashTime);

        RefreshFromStack();
    }

    private void RefreshFromStack()
    {
        var top = _player.PowerUps != null
            ? _player.PowerUps.TopKey()
            : PowerUpSO.PowerUpKey.None;

        if (top == PowerUpSO.PowerUpKey.None)
        {
            ReleaseRing();
            return;
        }
        EnsureRing();
        _ringColor = PowerUpVisuals.GetTint(top);
        _ringColor.a = alpha;
        ApplyRingColor();
    }

    // ── Ring lifecycle ──────────────────────────────────────────────────────
    private void EnsureRing()
    {
        if (_ring != null) return;
        _ring = PrimitivePool.AcquireSphere();
        if (_ring.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
        if (_sharedMat == null) _sharedMat = new Material(ShaderHelper.GetUnlit());
        if (_mpb == null)       _mpb       = new MaterialPropertyBlock();
        var rend = _ring.GetComponent<Renderer>();
        rend.sharedMaterial = _sharedMat;
        _spawnTime = Time.time;
    }

    private void ApplyRingColor()
    {
        if (_ring == null) return;
        var rend = _ring.GetComponent<Renderer>();
        _mpb.Clear();
        _mpb.SetColor(BaseColorId, _ringColor);
        _mpb.SetColor(ColorId,     _ringColor);
        rend.SetPropertyBlock(_mpb);
    }

    private void ReleaseRing()
    {
        if (_ring == null) return;
        PrimitivePool.Release(_ring);
        _ring = null;
    }

    private void LateUpdate()
    {
        if (_ring == null || _player == null) return;

        Vector3 pos = _player.transform.position;
        pos.y = ringHeight * 0.5f;

        float t = (Time.time - _spawnTime) * pulseHz * Mathf.PI * 2f;
        float pulse = 1f + Mathf.Sin(t) * pulseAmp;
        float xz = ringScale * pulse;

        _ring.transform.position   = pos;
        _ring.transform.localScale = new Vector3(xz, ringHeight, xz);
    }
}
