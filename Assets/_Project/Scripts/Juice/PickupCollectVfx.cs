using UnityEngine;

/// <summary>
/// Burst ring at pickup world-pos when a power-up is collected. Reads the
/// effect's tint via <see cref="PowerUpVisuals"/>. Lives on the scene-baked
/// <c>[Juice]</c> GameObject (added by <c>JuiceSetupAutomation</c>).
/// </summary>
public class PickupCollectVfx : MonoBehaviour
{
    [Header("Burst")]
    [SerializeField] private int   particleCount = 12;
    [SerializeField] private float particleScale = 0.18f;
    [SerializeField] private float burstSpeed    = 7f;
    [SerializeField] private float lifetime      = 0.5f;
    [SerializeField] private float upwardBias    = 0.6f;

    private static Material _sharedMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        GameEvents.OnPickupCollected += HandlePickupCollected;
    }

    private void OnDisable()
    {
        GameEvents.OnPickupCollected -= HandlePickupCollected;
    }

    private void HandlePickupCollected(int playerIndex, int powerUpKey, Vector3 worldPos)
    {
        Color tint = PowerUpVisuals.GetTint(powerUpKey);
        Burst(worldPos, tint);
    }

    private void Burst(Vector3 pos, Color color)
    {
        if (_sharedMat == null) _sharedMat = new Material(ShaderHelper.GetLit());
        if (_mpb == null)       _mpb       = new MaterialPropertyBlock();

        for (int i = 0; i < particleCount; i++)
        {
            var go = PrimitivePool.AcquireSphere();
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * particleScale * Random.Range(0.7f, 1.2f);

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = _sharedMat;
            _mpb.Clear();
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId,     color);
            rend.SetPropertyBlock(_mpb);

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;

            // Outward fan in flat plane plus upward kick.
            float angle = i * (360f / particleCount) + Random.Range(-8f, 8f);
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            dir.y = upwardBias + Random.Range(-0.1f, 0.2f);
            rb.linearVelocity  = dir.normalized * burstSpeed * Random.Range(0.8f, 1.2f);
            rb.angularVelocity = Random.insideUnitSphere * 8f;

            PrimitivePool.ReleaseAfter(go, lifetime);
        }
    }
}
