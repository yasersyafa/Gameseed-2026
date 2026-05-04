using System;
using DG.Tweening;
using UnityEngine;
using VContainer;

public class MeleeController : MonoBehaviour
{
    [Header("Swing Settings")]
    [SerializeField] private float swingAngle    = 120f; // total arc
    [SerializeField] private float swingDuration = 0.3f;
    [SerializeField] private Ease  swingEase     = Ease.OutQuart;

    [Header("Parry")]
    [SerializeField] private float parrySpeedMultiplier = 1.4f;

    private Tween             _swingTween;
    private HitEffectManager  _hits;

    // Sword has no Rigidbody → it is a compound collider under the player's
    // Rigidbody, so OnCollisionEnter never fires on this GameObject. Use
    // explicit overlap polling during the active swing window instead.
    private Collider _swordCollider;
    private bool     _swinging;
    private bool     _hasHitThisSwing;
    private static readonly Collider[] _hitBuffer = new Collider[8];

    [Inject]
    public void Construct(HitEffectManager hits)
    {
        _hits = hits;
    }

    private void Awake()
    {
        // Collider lives on a child GameObject (parent only carries the script
        // and drives swing rotation). Include inactive — sword starts disabled.
        _swordCollider = GetComponentInChildren<Collider>(true);
    }

    /// <summary>
    /// Aktifkan sword, swing horizontal, lalu nonaktifkan.
    /// onComplete dipanggil setelah sword nonaktif.
    /// </summary>
    public void Swing(Action onComplete)
    {
        gameObject.SetActive(true);
        _hasHitThisSwing = false;
        _swinging        = true;

        // Reset rotasi lokal ke posisi awal swing
        float startAngle = swingAngle * 0.5f;
        float endAngle   = -swingAngle * 0.5f;
        transform.localEulerAngles = new Vector3(0f, startAngle, 0f);

        _swingTween?.Kill();
        _swingTween = transform
            .DOLocalRotate(new Vector3(0f, endAngle, 0f), swingDuration)
            .SetEase(swingEase)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(() =>
            {
                if (this == null || gameObject == null) return;
                _swinging = false;
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    private void OnDestroy()
    {
        _swingTween?.Kill();
        transform.DOKill();
    }

    public void CancelSwing()
    {
        _swingTween?.Kill();
        _swinging = false;
        gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        if (!_swinging || _hasHitThisSwing || _swordCollider == null) return;

        // Use the collider's own transform — collider is on a child GO that
        // inherits the swing rotation from this parent.
        Transform colTf = _swordCollider.transform;

        int n;
        if (_swordCollider is BoxCollider box)
        {
            Vector3 center = colTf.TransformPoint(box.center);
            Vector3 half   = Vector3.Scale(box.size * 0.5f, colTf.lossyScale);
            n = Physics.OverlapBoxNonAlloc(center, half, _hitBuffer, colTf.rotation,
                                           ~0, QueryTriggerInteraction.Collide);
        }
        else
        {
            // Fallback for sphere/capsule/mesh — axis-aligned bounds query.
            var b = _swordCollider.bounds;
            n = Physics.OverlapBoxNonAlloc(b.center, b.extents, _hitBuffer, Quaternion.identity,
                                           ~0, QueryTriggerInteraction.Collide);
        }

        for (int i = 0; i < n; i++)
        {
            if (HandleHit(_hitBuffer[i]))
            {
                _hasHitThisSwing = true;
                return;
            }
        }
    }

    private bool HandleHit(Collider other)
    {
        if (other == null) return false;

        var attacker = transform.root.GetComponent<PlayerController>();
        if (attacker == null) return false;

        // Parry: boomerang masuk swing window → reflect ke arah parrier facing.
        // GetComponentInParent karena boomerang collider mungkin di child mesh.
        var boomerang = other.GetComponentInParent<Boomerang>();
        if (boomerang != null)
        {
            // Skip parry boomerang sendiri
            if (boomerang.Owner == attacker.transform) return false;

            Vector3 reflectDir = attacker.LastMoveDirection.sqrMagnitude > 0.01f
                ? attacker.LastMoveDirection.normalized
                : attacker.transform.forward;

            boomerang.Reflect(reflectDir, parrySpeedMultiplier, attacker.transform);
            boomerang.SetThrowerIndex(attacker.PlayerIndex);
            attacker.ActiveBoomerang = boomerang;

            GameEvents.RaiseBoomerangParried(attacker.PlayerIndex);

#if UNITY_EDITOR
            Debug.Log($"[Melee] PARRY by player {attacker.PlayerIndex}");
#endif
            gameObject.SetActive(false);
            return true;
        }

        // Player hit: walk up to find PlayerController (collider may be on child).
        var victim = other.GetComponentInParent<PlayerController>();
        if (victim != null && victim != attacker)
        {
            Vector3 hitDir = (victim.transform.position - attacker.transform.position).normalized;
            victim.OnHitByBoomerang(attacker.PlayerIndex, hitDir);
#if UNITY_EDITOR
            Debug.Log($"[Melee] HIT by player {attacker.PlayerIndex} → victim {victim.PlayerIndex}");
#endif
            gameObject.SetActive(false);
            return true;
        }

        return false;
    }

    public float SwingDuration => swingDuration;
}
