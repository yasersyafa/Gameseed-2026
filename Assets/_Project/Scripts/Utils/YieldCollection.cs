using System.Collections.Generic;
using UnityEngine;


public static class YieldCollection
{
    // ── WaitForSeconds Cache ──────────────────────────────────────────────────

    private static readonly Dictionary<float, WaitForSeconds> _waitForSeconds = new();
    private static readonly Dictionary<float, WaitForSecondsRealtime> _waitForSecondsRealtime = new();

    public static WaitForSeconds WaitForSeconds(float seconds)
    {
        if (!_waitForSeconds.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSeconds(seconds);
            _waitForSeconds[seconds] = wait;
        }
        return wait;
    }

    public static WaitForSecondsRealtime WaitForSecondsRealtime(float seconds)
    {
        if (!_waitForSecondsRealtime.TryGetValue(seconds, out var wait))
        {
            wait = new WaitForSecondsRealtime(seconds);
            _waitForSecondsRealtime[seconds] = wait;
        }
        return wait;
    }

    // ── Singleton Yields ──────────────────────────────────────────────────────

    public static readonly WaitForEndOfFrame EndOfFrame = new();
    public static readonly WaitForFixedUpdate FixedUpdate = new();

    // ── WaitUntil / WaitWhile ──────────────────

    public static WaitUntil Until(System.Func<bool> predicate) => new(predicate);
    public static WaitWhile While(System.Func<bool> predicate) => new(predicate);
}