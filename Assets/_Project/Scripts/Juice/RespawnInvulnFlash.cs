using System.Collections;
using UnityEngine;

/// <summary>
/// I-frame tell saat respawn — toggle visual.enabled cepat selama window invuln,
/// supaya pemain tahu mereka baru hidup. Subscribe per-player ke OnPlayerRespawned.
/// Pure visual; LivesSystem belum punya invuln logic — ini cue, bukan gameplay.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class RespawnInvulnFlash : MonoBehaviour
{
    [SerializeField] private float duration       = 1.5f;
    [SerializeField] private float toggleInterval = 0.08f;

    private PlayerController _player;
    private Coroutine        _routine;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerRespawned += HandleRespawn;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerRespawned -= HandleRespawn;
        if (_routine != null) StopCoroutine(_routine);
        if (_player != null && _player.visual != null) _player.visual.enabled = true;
    }

    private void HandleRespawn(int idx, PlayerController controller)
    {
        if (controller != _player) return;
        if (_player.visual == null) return;
        if (_routine != null) StopCoroutine(_routine);
        _routine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        var visual = _player.visual;
        float t = 0f;
        while (t < duration)
        {
            visual.enabled = !visual.enabled;
            yield return YieldCollection.WaitForSeconds(toggleInterval);
            t += toggleInterval;
        }
        visual.enabled = true;
        _routine = null;
    }
}
