using System.Collections;
using UnityEngine;

/// <summary>
/// Ribbon trail enabled while dashing. Auto-attached by PlayerController.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class DashTrail : MonoBehaviour
{
    [SerializeField] private float trailTime  = 0.25f;
    [SerializeField] private float startWidth = 0.6f;
    [SerializeField] private float endWidth   = 0f;

    private TrailRenderer    _trail;
    private PlayerController _player;
    private Coroutine        _activeRoutine;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();

        var trailGo = new GameObject("DashTrail");
        trailGo.transform.SetParent(transform, false);
        _trail = trailGo.AddComponent<TrailRenderer>();
        _trail.time       = trailTime;
        _trail.startWidth = startWidth;
        _trail.endWidth   = endWidth;
        _trail.emitting   = false;

        _trail.sharedMaterial = ShaderHelper.SharedUnlit();
    }

    private void Start()
    {
        var rend = _player != null ? _player.visual : null;
        if (rend != null && _trail != null)
        {
            Color c = rend.sharedMaterial != null ? rend.sharedMaterial.color : Color.white;
            _trail.startColor = new Color(c.r, c.g, c.b, 0.9f);
            _trail.endColor   = new Color(c.r, c.g, c.b, 0f);
            // No mutation of sharedMaterial.color — that would tint every
            // trail in the scene. Per-trail color comes from start/endColor.
        }
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
        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(EmitDuration(_player.DashDuration));
    }

    private IEnumerator EmitDuration(float dur)
    {
        _trail.emitting = true;
        yield return YieldCollection.WaitForSeconds(dur);
        _trail.emitting = false;
        _activeRoutine = null;
    }
}
