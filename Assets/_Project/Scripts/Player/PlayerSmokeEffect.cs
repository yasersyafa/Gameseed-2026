using UnityEngine;

/// <summary>
/// Spawn smoke particle burst on dash. Subscribes OnPlayerDashed.
/// Uses PrimitivePool — no per-dash GameObject.CreatePrimitive.
/// Auto-attached by PlayerController.EnsureRuntimeComponents.
/// </summary>
public class PlayerSmokeEffect : MonoBehaviour
{
    [Header("Smoke")]
    [SerializeField] private int   particleCount  = 12;
    [SerializeField] private float burstRadius    = 0.4f;
    [SerializeField] private float particleScale  = 0.18f;
    [SerializeField] private float lifetime       = 0.5f;
    [SerializeField] private Color smokeColor     = new(0.85f, 0.85f, 0.85f, 0.85f);

    private PlayerController _player;
    private static Material _sharedMat;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDashed += HandleDash;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDashed -= HandleDash;
    }

    private void HandleDash(int idx)
    {
        if (_player == null || _player.PlayerIndex != idx) return;
        SpawnBurst(transform.position + Vector3.down * 0.4f);
    }

    private void SpawnBurst(Vector3 origin)
    {
        if (_sharedMat == null)
        {
            _sharedMat = new Material(ShaderHelper.GetUnlit())
            {
                color = smokeColor,
            };
        }

        for (int i = 0; i < particleCount; i++)
        {
            var go = PrimitivePool.AcquireSphere();

            Vector3 dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 0.3f;
            go.transform.position   = origin + dir * burstRadius * 0.3f;
            go.transform.localScale = Vector3.one * particleScale;

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = _sharedMat;

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.linearVelocity   = dir * Random.Range(1.5f, 3f);
            rb.angularVelocity  = Random.insideUnitSphere * 4f;

            PrimitivePool.ReleaseAfter(go, lifetime);
        }
    }
}
