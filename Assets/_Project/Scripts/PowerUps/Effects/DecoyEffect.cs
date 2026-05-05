using UnityEngine;

/// <summary>
/// Spawns a single Player-tagged dummy at a slight backward offset. The dummy
/// has a Collider so opposing boomerangs collide with it (treating it as a
/// player and consuming the projectile). The dummy destroys itself on first
/// collision via <see cref="DecoyClone"/>.
/// </summary>
public class DecoyEffect : IPowerUpEffect
{
    public PowerUpSO.PowerUpKey Key      => PowerUpSO.PowerUpKey.Decoy;
    public PowerUpSO.MutexGroup Mutex    => PowerUpSO.MutexGroup.None;
    public float                Duration { get; }

    private GameObject _instance;

    public DecoyEffect(float duration = 10f) { Duration = duration; }

    public void OnApply(PlayerController player)
    {
        Vector3 back = -player.LastMoveDirection;
        if (back.sqrMagnitude < 0.001f) back = -player.transform.forward;
        Vector3 pos = player.transform.position + back.normalized * 1.6f;

        _instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
        _instance.name = $"Decoy_P{player.PlayerIndex}";
        _instance.tag = "Player";  // boomerang treats it as a kill target
        _instance.transform.position   = pos;
        _instance.transform.localScale = player.transform.localScale;

        // Kinematic rigidbody so collision callbacks fire when the dynamic
        // boomerang hits the decoy. Two kinematic bodies wouldn't generate
        // OnCollisionEnter; boomerang is dynamic so the pair works.
        var rb = _instance.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity  = false;

        // Mirror player tint so opponents can't tell at a glance.
        if (player.visual != null && player.visual.sharedMaterial != null)
        {
            var rend = _instance.GetComponent<Renderer>();
            var mpb  = new MaterialPropertyBlock();
            mpb.SetColor("_BaseColor", player.visual.sharedMaterial.color);
            mpb.SetColor("_Color",     player.visual.sharedMaterial.color);
            rend.SetPropertyBlock(mpb);
        }

        _instance.AddComponent<DecoyClone>();
    }

    public void OnRemove(PlayerController player)
    {
        if (_instance != null) Object.Destroy(_instance);
        _instance = null;
    }

    public void OnBeforeThrow(PlayerController player, Boomerang boomerang) { }
    public bool OnBeforeHit(PlayerController player, int killerIndex, Vector3 hitDir) => false;
    public bool OnBeforeDash(PlayerController player) => false;
    public void Tick(PlayerController player) { }
}

/// <summary>Self-destruct on first collision (boomerang impact).</summary>
public class DecoyClone : MonoBehaviour
{
    private void OnCollisionEnter(Collision _)  => Object.Destroy(gameObject);
    private void OnTriggerEnter(Collider _)     => Object.Destroy(gameObject);
}
