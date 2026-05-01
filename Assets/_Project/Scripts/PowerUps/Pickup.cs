using DG.Tweening;
using UnityEngine;

/// <summary>
/// World object berisi 1 power-up. OnTriggerEnter player → apply ke
/// PowerUpController, fire OnPickupCollected event, destroy diri.
/// Visual: rotate + bob + glow pulse (DOTween).
/// </summary>
[RequireComponent(typeof(Collider))]
public class Pickup : MonoBehaviour
{
    [Header("Power-up data")]
    [SerializeField] private PowerUpSO  powerUpSO;

    [Header("Visual")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobAmplitude = 0.25f;
    [SerializeField] private float bobFrequency = 1.5f;
    [SerializeField] private Renderer visual;

    private Vector3   _basePos;
    private float     _spawnTime;

    public PowerUpSO Data => powerUpSO;

    public void Configure(PowerUpSO so)
    {
        powerUpSO = so;
        if (visual != null && so != null)
            visual.material.color = so.tint;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        col.isTrigger = true;
        _basePos   = transform.position;
        _spawnTime = Time.time;
    }

    private void Start()
    {
        if (visual != null && powerUpSO != null)
            visual.material.color = powerUpSO.tint;

        // Glow pulse via tween
        if (visual != null)
        {
            visual.transform.DOScale(visual.transform.localScale * 1.15f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(visual.gameObject, LinkBehaviour.KillOnDestroy);
        }
    }

    private void OnDestroy()
    {
        if (visual != null)
            visual.transform.DOKill();
    }

    private void Update()
    {
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        float t   = Time.time - _spawnTime;
        float y   = _basePos.y + Mathf.Sin(t * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        var effect = PowerUpFactory.Create(powerUpSO);
        if (effect == null) return;

        bool applied = player.PowerUps?.Apply(effect) ?? false;
        if (applied)
        {
#if UNITY_EDITOR
            Debug.Log($"[Pickup] {powerUpSO.key} → Player {player.PlayerIndex}");
#endif
            Destroy(gameObject);
        }
    }
}
