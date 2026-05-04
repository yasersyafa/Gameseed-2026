using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static pool for runtime-spawned primitive GameObjects (smoke, splat chunks).
/// Avoids GameObject.CreatePrimitive churn + collider creation overhead.
/// Acquire → reset → use → ReturnAfter(seconds).
/// </summary>
public static class PrimitivePool
{
    private const int InitialCubeCapacity   = 32;
    private const int InitialSphereCapacity = 32;

    private static readonly Queue<GameObject> _spheres = new();
    private static readonly Queue<GameObject> _cubes   = new();
    private static Transform _root;

    private static Transform Root
    {
        get
        {
            if (_root == null)
            {
                var go = new GameObject("[PrimitivePool]");
                Object.DontDestroyOnLoad(go);
                _root = go.transform;
            }
            return _root;
        }
    }

    public static GameObject AcquireSphere() => Acquire(_spheres, PrimitiveType.Sphere);
    public static GameObject AcquireCube()   => Acquire(_cubes,   PrimitiveType.Cube);

    private static GameObject Acquire(Queue<GameObject> pool, PrimitiveType type)
    {
        while (pool.Count > 0)
        {
            var go = pool.Dequeue();
            if (go == null) continue;
            go.SetActive(true);
            return go;
        }

        var fresh = GameObject.CreatePrimitive(type);
        var col   = fresh.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        fresh.transform.SetParent(Root, false);
        return fresh;
    }

    public static void Release(GameObject go)
    {
        if (go == null) return;
        ResetState(go);
        go.SetActive(false);
        go.transform.SetParent(Root, false);

        // route by mesh name (Unity primitives keep their mesh name)
        var filter = go.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null && filter.sharedMesh.name == "Cube") _cubes.Enqueue(go);
        else _spheres.Enqueue(go);
    }

    public static void ReleaseAfter(GameObject go, float seconds)
    {
        if (go == null) return;
        var releaser = go.GetComponent<PoolReleaser>();
        if (releaser == null) releaser = go.AddComponent<PoolReleaser>();
        releaser.Schedule(seconds);
    }

    private static void ResetState(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic     = false;
            rb.useGravity      = true;
        }
        go.transform.localScale = Vector3.one;
    }

    public static void Prewarm()
    {
        for (int i = 0; i < InitialSphereCapacity; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            _spheres.Enqueue(go);
        }
        for (int i = 0; i < InitialCubeCapacity; i++)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.Destroy(col);
            go.SetActive(false);
            go.transform.SetParent(Root, false);
            _cubes.Enqueue(go);
        }
    }
}

/// <summary>Auto-returns owning GameObject to PrimitivePool after timer.</summary>
public class PoolReleaser : MonoBehaviour
{
    private float _scheduledAt;
    private float _delay;
    private bool  _active;

    public void Schedule(float seconds)
    {
        _scheduledAt = Time.time;
        _delay       = seconds;
        _active      = true;
    }

    private void Update()
    {
        if (!_active) return;
        if (Time.time - _scheduledAt < _delay) return;
        _active = false;
        PrimitivePool.Release(gameObject);
    }
}
