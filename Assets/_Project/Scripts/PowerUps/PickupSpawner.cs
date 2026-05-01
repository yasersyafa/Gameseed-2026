using System.Collections;
using UnityEngine;

/// <summary>
/// Spawn pickup di anchor random per interval. Pilih random dari pool
/// PowerUpSO. Cap concurrent active pickups.
/// </summary>
public class PickupSpawner : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private GameObject pickupPrefab;
    [SerializeField] private Transform[] anchors;
    [SerializeField] private PowerUpSO[] pool;

    [Header("Timing")]
    [SerializeField] private float spawnInterval = 5f;
    [SerializeField] private float initialDelay  = 3f;

    [Header("Cap")]
    [SerializeField] private int maxConcurrent = 3;

    private readonly System.Collections.Generic.List<GameObject> _active = new();

    private void OnEnable()
    {
        GameEvents.OnRoundStarted += HandleRoundStarted;
        GameEvents.OnRoundEnded   += HandleRoundEnded;
    }

    private void OnDisable()
    {
        GameEvents.OnRoundStarted -= HandleRoundStarted;
        GameEvents.OnRoundEnded   -= HandleRoundEnded;
    }

    private void HandleRoundStarted(int round)
    {
        StopAllCoroutines();
        StartCoroutine(SpawnLoop());
    }

    private void HandleRoundEnded(int winnerIndex)
    {
        StopAllCoroutines();
        ClearAllActive();
    }

    private IEnumerator SpawnLoop()
    {
        yield return YieldCollection.WaitForSeconds(initialDelay);

        while (true)
        {
            CleanupDestroyed();

            if (_active.Count < maxConcurrent)
                SpawnOne();

            yield return YieldCollection.WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnOne()
    {
        if (pickupPrefab == null || anchors == null || anchors.Length == 0
            || pool == null || pool.Length == 0) return;

        Transform anchor = anchors[Random.Range(0, anchors.Length)];
        PowerUpSO so     = pool[Random.Range(0, pool.Length)];

        var go = Instantiate(pickupPrefab, anchor.position, Quaternion.identity);
        var pickup = go.GetComponent<Pickup>();
        pickup?.Configure(so);
        _active.Add(go);

        GameEvents.RaisePickupSpawned();

#if UNITY_EDITOR
        Debug.Log($"[Spawner] spawn {so.key} @ {anchor.name}");
#endif
    }

    private void CleanupDestroyed()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
            if (_active[i] == null) _active.RemoveAt(i);
    }

    private void ClearAllActive()
    {
        for (int i = 0; i < _active.Count; i++)
            if (_active[i] != null) Destroy(_active[i]);
        _active.Clear();
    }
}
