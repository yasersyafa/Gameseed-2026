using System;
using DG.Tweening;
using UnityEngine;

public class MeleeController : MonoBehaviour
{
    [Header("Swing Settings")]
    [SerializeField] private float swingAngle    = 120f; // total arc
    [SerializeField] private float swingDuration = 0.3f;
    [SerializeField] private Ease  swingEase     = Ease.OutQuart;

    private Tween _swingTween;

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
        if (collision.gameObject.TryGetComponent<PlayerController>(out var player))
        {
            player.OnHitByBoomerang();
            gameObject.SetActive(false);
        }
    }

    public float SwingDuration => swingDuration;
}