using UnityEngine;

/// <summary>
/// Confetti burst di posisi winner saat OnGameOver. Pakai PrimitivePool cubes
/// dengan random palette + winner color emphasis. Berdiri sendiri, tidak butuh
/// scene wiring (di-spawn lewat JuiceBootstrap).
/// </summary>
public class WinFlourish : MonoBehaviour
{
    [Header("Confetti")]
    [SerializeField] private int   confettiCount = 36;
    [SerializeField] private float confettiScale = 0.18f;
    [SerializeField] private float burstSpeed    = 11f;
    [SerializeField] private float lifetime      = 2.0f;
    [SerializeField] private float spawnHeight   = 1.4f;

    [Header("Palette")]
    [SerializeField] private Color[] palette = {
        new(1f, 0.85f, 0.2f),
        new(1f, 0.4f, 0.5f),
        new(0.4f, 0.85f, 1f),
        new(0.6f, 1f, 0.5f),
        new(1f, 1f, 1f),
    };

    private static Material _sharedMat;
    private static MaterialPropertyBlock _mpb;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    private void OnEnable()
    {
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver(int winnerIndex)
    {
        Vector3 spawnPos = ResolveWinnerPos(winnerIndex);
        Burst(spawnPos, ResolveWinnerColor(winnerIndex));
    }

    private Vector3 ResolveWinnerPos(int winnerIndex)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
            if (players[i].PlayerIndex == winnerIndex)
                return players[i].transform.position + Vector3.up * spawnHeight;
        return Vector3.up * spawnHeight;
    }

    private Color ResolveWinnerColor(int winnerIndex)
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        for (int i = 0; i < players.Length; i++)
            if (players[i].PlayerIndex == winnerIndex
                && players[i].visual != null
                && players[i].visual.sharedMaterial != null)
                return players[i].visual.sharedMaterial.color;
        return Color.white;
    }

    private void Burst(Vector3 pos, Color winnerColor)
    {
        if (_sharedMat == null) _sharedMat = new Material(ShaderHelper.GetLit());
        if (_mpb == null)       _mpb       = new MaterialPropertyBlock();

        for (int i = 0; i < confettiCount; i++)
        {
            var go = PrimitivePool.AcquireCube();
            go.transform.position   = pos;
            go.transform.localScale = new Vector3(
                confettiScale * Random.Range(0.5f, 1.0f),
                confettiScale * Random.Range(0.15f, 0.35f),
                confettiScale * Random.Range(0.5f, 1.0f));

            var rend = go.GetComponent<Renderer>();
            rend.sharedMaterial = _sharedMat;

            // 30% chance untuk pakai winner color, sisanya palette
            Color c = (i % 3 == 0) ? winnerColor : palette[Random.Range(0, palette.Length)];
            _mpb.Clear();
            _mpb.SetColor(BaseColorId, c);
            _mpb.SetColor(ColorId,     c);
            rend.SetPropertyBlock(_mpb);

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.useGravity = true;

            Vector3 dir = Random.insideUnitSphere;
            dir.y = Mathf.Abs(dir.y) * 1.2f + 0.6f;
            rb.linearVelocity  = dir.normalized * burstSpeed * Random.Range(0.7f, 1.3f);
            rb.angularVelocity = Random.insideUnitSphere * 18f;

            PrimitivePool.ReleaseAfter(go, lifetime);
        }
    }
}
