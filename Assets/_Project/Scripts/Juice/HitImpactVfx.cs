using UnityEngine;

/// <summary>
/// Directional impact burst saat player kena boomerang. Spawn pooled spheres
/// dari posisi player ke arah hit (fan kecil) supaya hitnya kerasa punchy.
/// Subscribe global ke OnPlayerHit + OnBoomerangParried.
/// </summary>
public class HitImpactVfx : MonoBehaviour
{
    [Header("Burst")]
    [SerializeField] private int   particleCount   = 10;
    [SerializeField] private float particleScale   = 0.18f;
    [SerializeField] private float burstSpeed      = 9f;
    [SerializeField] private float lifetime        = 0.45f;
    [SerializeField] private float spreadDegrees   = 55f;
    [SerializeField] private float upwardBias      = 0.4f;
    [SerializeField] private Color defaultColor    = new(1f, 0.85f, 0.3f, 1f);
    [SerializeField] private Color parryColor      = new(0.5f, 0.9f, 1f, 1f);

    private static Material _sharedMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private static readonly Color shieldColor = new(1.00f, 0.92f, 0.40f);

    private void OnEnable()
    {
        GameEvents.OnPlayerHit      += HandleHit;
        GameEvents.OnShieldAbsorbed += HandleShieldAbsorbed;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerHit      -= HandleHit;
        GameEvents.OnShieldAbsorbed -= HandleShieldAbsorbed;
    }

    private void HandleHit(int idx, PlayerController controller)
    {
        if (controller == null) return;
        Vector3 pos = controller.transform.position + Vector3.up * 0.6f;
        Vector3 dir = controller.LastMoveDirection;
        if (dir.sqrMagnitude < 0.001f) dir = controller.transform.forward;
        Burst(pos, -dir, defaultColor);
    }

    private void HandleShieldAbsorbed(int idx, Vector3 hitDir)
    {
        var ctrl = ResolvePlayer(idx);
        if (ctrl == null) return;
        Vector3 pos = ctrl.transform.position + Vector3.up * 0.6f;
        Vector3 dir = hitDir.sqrMagnitude > 0.001f ? -hitDir.normalized : Vector3.forward;
        Burst(pos, dir, shieldColor);
    }

    private static PlayerController ResolvePlayer(int idx)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
            if (players[i].PlayerIndex == idx) return players[i];
        return null;
    }

    public void Burst(Vector3 pos, Vector3 forward, Color color)
    {
        if (_sharedMat == null) _sharedMat = new Material(ShaderHelper.GetLit());
        if (_mpb == null)       _mpb       = new MaterialPropertyBlock();

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        for (int i = 0; i < particleCount; i++)
        {
            var go = PrimitivePool.AcquireSphere();
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * particleScale * Random.Range(0.7f, 1.3f);

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = _sharedMat;
            _mpb.Clear();
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId,     color);
            rend.SetPropertyBlock(_mpb);

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;

            float yaw = Random.Range(-spreadDegrees, spreadDegrees);
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * forward;
            dir.y = upwardBias + Random.Range(-0.1f, 0.2f);
            rb.linearVelocity  = dir.normalized * burstSpeed * Random.Range(0.7f, 1.25f);
            rb.angularVelocity = Random.insideUnitSphere * 10f;

            PrimitivePool.ReleaseAfter(go, lifetime);
        }
    }
}
