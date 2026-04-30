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

    [Inject]
    public void Construct(HitEffectManager hits)
    {
        _hits = hits;
    }

    /// <summary>
    /// Aktifkan sword, swing horizontal, lalu nonaktifkan.
    /// onComplete dipanggil setelah sword nonaktif.
    /// </summary>
    public void Swing(Action onComplete)
    {
        gameObject.SetActive(true);

        // Reset rotasi lokal ke posisi awal swing
        float startAngle = swingAngle * 0.5f;
        float endAngle   = -swingAngle * 0.5f;
        transform.localEulerAngles = new Vector3(0f, startAngle, 0f);

        _swingTween?.Kill();
        _swingTween = transform
            .DOLocalRotate(new Vector3(0f, endAngle, 0f), swingDuration)
            .SetEase(swingEase)
            .OnComplete(() =>
            {
                gameObject.SetActive(false);
                onComplete?.Invoke();
            });
    }

    public void CancelSwing()
    {
        _swingTween?.Kill();
        gameObject.SetActive(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Parry: boomerang masuk swing window → reflect ke arah parrier facing
        if (collision.gameObject.TryGetComponent<Boomerang>(out var boomerang))
        {
            var parrier = transform.root.GetComponent<PlayerController>();
            if (parrier == null) return;

            // Skip parry boomerang sendiri
            if (boomerang.Owner == parrier.transform) return;

            Vector3 reflectDir = parrier.transform.forward;
            if (parrier.LastMoveDirection.sqrMagnitude > 0.01f)
                reflectDir = parrier.LastMoveDirection.normalized;

            boomerang.Reflect(reflectDir, parrySpeedMultiplier, parrier.transform);
            boomerang.SetThrowerIndex(parrier.PlayerIndex);

            // Parrier sekarang punya boomerang baru ini sebagai active
            parrier.ActiveBoomerang = boomerang;

            _hits?.TriggerKillEffect();
            GameEvents.RaiseBoomerangParried(parrier.PlayerIndex);

#if UNITY_EDITOR
            Debug.Log($"[Melee] PARRY by player {parrier.PlayerIndex}");
#endif
            gameObject.SetActive(false);
            return;
        }

        if (collision.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            Vector3 hitDir = (collision.transform.position - transform.position).normalized;
            int killerIndex = transform.root.GetComponent<PlayerController>()?.PlayerIndex ?? -1;
            player.OnHitByBoomerang(killerIndex, hitDir);
            gameObject.SetActive(false);
        }
    }

    public float SwingDuration => swingDuration;
}