using UnityEngine;

/// <summary>
/// Ground line previewing throw direction + distance during charge.
/// LineRenderer child auto-built; visible only while charging.
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class AimIndicator : MonoBehaviour
{
    [Header("Visual")]
    [SerializeField] private float yLift          = 0.05f;
    [SerializeField] private float widthMin       = 0.15f;
    [SerializeField] private float widthMax       = 0.5f;
    [SerializeField] private Color colorMin       = new(1f, 1f, 1f, 0.9f);
    [SerializeField] private Color colorMax       = new(1f, 0.4f, 0.2f, 1f);

    private PlayerController _player;
    private LineRenderer     _line;
    private bool             _charging;
    private float            _chargeStartTime;

    private void Awake()
    {
        _player = GetComponent<PlayerController>();

        var go = new GameObject("AimLine");
        go.transform.SetParent(transform, false);
        _line = go.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.useWorldSpace = true;
        _line.startWidth    = widthMin;
        _line.endWidth      = widthMin;
        _line.numCapVertices = 4;

        _line.sharedMaterial = ShaderHelper.SharedUnlit();
        _line.startColor = colorMin;
        _line.endColor   = new Color(colorMin.r, colorMin.g, colorMin.b, 0f);

        _line.enabled = false;
    }

    private void OnEnable()
    {
        GameEvents.OnChargeStarted   += HandleChargeStart;
        GameEvents.OnChargeReleased  += HandleChargeRelease;
        GameEvents.OnPlayerDashed    += HandleDash;
        GameEvents.OnPlayerEliminated += HandleElim;
    }

    private void OnDisable()
    {
        GameEvents.OnChargeStarted   -= HandleChargeStart;
        GameEvents.OnChargeReleased  -= HandleChargeRelease;
        GameEvents.OnPlayerDashed    -= HandleDash;
        GameEvents.OnPlayerEliminated -= HandleElim;
    }

    private bool IsMine(int idx) => _player != null && _player.PlayerIndex == idx;

    private void HandleChargeStart(int idx)
    {
        if (!IsMine(idx)) return;
        _charging        = true;
        _chargeStartTime = Time.time;
        _line.enabled    = true;
    }

    private void HandleChargeRelease(int idx, float charge01) { if (IsMine(idx)) Hide(); }
    private void HandleDash(int idx)                          { if (IsMine(idx)) Hide(); }
    private void HandleElim(int idx, PlayerController c)      { if (IsMine(idx)) Hide(); }

    private void Hide()
    {
        _charging = false;
        if (_line != null) _line.enabled = false;
    }

    private void LateUpdate()
    {
        if (!_charging || _line == null || _player == null) return;

        float charge01 = Mathf.Clamp01((Time.time - _chargeStartTime) / Mathf.Max(0.01f, _player.ChargeMaxTime));
        float distance = Mathf.Lerp(_player.ChargeMinDistance, _player.ChargeMaxDistance, charge01);

        Vector3 dir = _player.LastMoveDirection;
        if (dir.sqrMagnitude < 0.001f) dir = transform.forward;
        dir = dir.normalized;

        Vector3 origin = transform.position + Vector3.up * yLift;
        Vector3 end    = origin + dir * distance;

        _line.SetPosition(0, origin);
        _line.SetPosition(1, end);

        float w = Mathf.Lerp(widthMin, widthMax, charge01);
        _line.startWidth = w;
        _line.endWidth   = w * 0.4f;

        Color c = Color.Lerp(colorMin, colorMax, charge01);
        _line.startColor = c;
        _line.endColor   = new Color(c.r, c.g, c.b, 0f);
    }

}
