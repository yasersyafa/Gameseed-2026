using DG.Tweening;
using UnityEngine;

/// <summary>
/// Death VFX: spawn colored chunks (food-splat) + dissolve visual on elimination.
/// Subscribes OnPlayerEliminated, only fires for own player index.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class DeathSplat : MonoBehaviour
{
    [Header("Splat")]
    [SerializeField] private int   chunkCount    = 14;
    [SerializeField] private float chunkScale    = 0.22f;
    [SerializeField] private float chunkLifetime = 1.2f;
    [SerializeField] private float burstForce    = 8f;
    [SerializeField] private float dissolveTime  = 0.6f;

    private PlayerController _player;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerEliminated += HandleEliminated;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerEliminated -= HandleEliminated;
    }

    private void HandleEliminated(int idx, PlayerController controller)
    {
        if (controller != _player) return;
        SpawnChunks();
        DissolveVisual();
    }

    private static Material              _sharedChunkMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int          BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int          ColorId     = Shader.PropertyToID("_Color");

    private void SpawnChunks()
    {
        Color color = (_player.visual != null && _player.visual.sharedMaterial != null)
            ? _player.visual.sharedMaterial.color
            : Color.white;

        if (_sharedChunkMat == null) _sharedChunkMat = new Material(ShaderHelper.GetLit());
        if (_mpb == null)            _mpb = new MaterialPropertyBlock();

        for (int i = 0; i < chunkCount; i++)
        {
            var go = Random.value > 0.5f
                ? PrimitivePool.AcquireCube()
                : PrimitivePool.AcquireSphere();

            go.transform.position   = transform.position + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * chunkScale * Random.Range(0.7f, 1.3f);

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = _sharedChunkMat;
            _mpb.Clear();
            _mpb.SetColor(BaseColorId, color);
            _mpb.SetColor(ColorId,     color);
            rend.SetPropertyBlock(_mpb);

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;
            Vector3 dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y) + 0.4f;
            rb.linearVelocity  = dir.normalized * burstForce * Random.Range(0.6f, 1.2f);
            rb.angularVelocity = Random.insideUnitSphere * 12f;

            PrimitivePool.ReleaseAfter(go, chunkLifetime);
        }
    }

    private void DissolveVisual()
    {
        if (_player.visual == null) return;

        Transform vt = _player.visual.transform;
        Vector3 baseScale = vt.localScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(vt.DOScale(baseScale * 1.4f, 0.08f).SetEase(Ease.OutBack))
           .Append(vt.DOScale(Vector3.zero, dissolveTime).SetEase(Ease.InBack))
           .SetLink(vt.gameObject, LinkBehaviour.KillOnDestroy)
           .OnComplete(() =>
           {
               if (vt != null) vt.localScale = baseScale;
           });
    }

    private void OnDestroy()
    {
        if (_player != null && _player.visual != null)
            _player.visual.transform.DOKill();
    }
}
