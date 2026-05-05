using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;

public class CinemachineCameraManager : MonoBehaviour
{
    public enum ShakeIntensity { Small, Medium, Large }

    [SerializeField] private CinemachineTargetGroup  targetGroup;
    [SerializeField] private CinemachineImpulseSource impulseSource;
    [SerializeField] private CinemachineCamera        vcam;

    [SerializeField] private float targetWeight = 1f;
    [SerializeField] private float targetRadius = 2f;

    [Header("Shake Profiles")]
    [SerializeField] private float smallShake  = 0.5f;
    [SerializeField] private float mediumShake = 1.5f;
    [SerializeField] private float largeShake  = 3.0f;

    [Header("FOV Punch (perspective only)")]
    [SerializeField] private float parryFovDelta = -8f;
    [SerializeField] private float parryFovTime  = 0.18f;
    [SerializeField] private float elimFovDelta  = -12f;
    [SerializeField] private float elimFovTime   = 0.45f;

    [Header("Last-Kill Stinger (OnGameOver)")]
    [SerializeField] private float stingerFovDelta = -22f;
    [SerializeField] private float stingerFovTime  = 1.2f;
    [SerializeField] private float stingerShake    = 4.5f;

    private SettingsManager _settings;
    private float           _baseFov;
    private Coroutine       _fovRoutine;

    [Inject]
    public void Construct(SettingsManager settings)
    {
        _settings = settings;
    }

    private void Awake()
    {
        if (vcam == null) vcam = FindFirstObjectByType<CinemachineCamera>();
        if (vcam != null) _baseFov = vcam.Lens.FieldOfView;
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerEliminated += HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  += HandlePlayerRespawned;
        GameEvents.OnPlayerHit        += HandlePlayerHit;
        GameEvents.OnBoomerangParried += HandleParry;
        GameEvents.OnBoomerangCaught  += HandleCatch;
        GameEvents.OnBoomerangWallBounce += HandleWallBounce;
        GameEvents.OnGameOver         += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerEliminated -= HandlePlayerEliminated;
        GameEvents.OnPlayerRespawned  -= HandlePlayerRespawned;
        GameEvents.OnPlayerHit        -= HandlePlayerHit;
        GameEvents.OnBoomerangParried -= HandleParry;
        GameEvents.OnBoomerangCaught  -= HandleCatch;
        GameEvents.OnBoomerangWallBounce -= HandleWallBounce;
        GameEvents.OnGameOver         -= HandleGameOver;
    }

    private void HandlePlayerEliminated(int index, PlayerController controller)
    {
        Shake(ShakeIntensity.Large);
        PunchFov(elimFovDelta, elimFovTime);
        RemoveTarget(controller.transform);
    }

    private void HandlePlayerRespawned(int index, PlayerController controller)
        => AddTargetIfNotExists(controller.transform);

    private void HandlePlayerHit(int index, PlayerController controller)
        => Shake(ShakeIntensity.Medium);

    private void HandleParry(int index)
    {
        Shake(ShakeIntensity.Medium);
        PunchFov(parryFovDelta, parryFovTime);
    }
    private void HandleCatch(int index)         => Shake(ShakeIntensity.Small);
    private void HandleWallBounce()             => Shake(ShakeIntensity.Small);

    private void HandleGameOver(int winnerIndex)
    {
        // Cinematic stinger: deeper FOV punch + extra shake. The target group
        // already auto-frames the lone surviving winner since losers were
        // removed on elim, so the lens just zooms toward them.
        ShakeCamera(stingerShake);
        PunchFov(stingerFovDelta, stingerFovTime);
    }

    public void PunchFov(float delta, float duration)
    {
        if (vcam == null) return;
        if (_fovRoutine != null) StopCoroutine(_fovRoutine);
        _fovRoutine = StartCoroutine(FovRoutine(delta, duration));
    }

    private IEnumerator FovRoutine(float delta, float duration)
    {
        const float attack = 0.05f;
        float target = _baseFov + delta;

        float t = 0f;
        while (t < attack)
        {
            t += Time.unscaledDeltaTime;
            var lens = vcam.Lens;
            lens.FieldOfView = Mathf.Lerp(_baseFov, target, t / attack);
            vcam.Lens = lens;
            yield return null;
        }

        float decay = Mathf.Max(0.05f, duration - attack);
        t = 0f;
        while (t < decay)
        {
            t += Time.unscaledDeltaTime;
            var lens = vcam.Lens;
            lens.FieldOfView = Mathf.Lerp(target, _baseFov, t / decay);
            vcam.Lens = lens;
            yield return null;
        }

        var done = vcam.Lens;
        done.FieldOfView = _baseFov;
        vcam.Lens = done;
        _fovRoutine = null;
    }

    public void AddTarget(Transform target)
        => targetGroup.AddMember(target, targetWeight, targetRadius);

    public void RemoveTarget(Transform target)
        => targetGroup.RemoveMember(target);

    public void Shake(ShakeIntensity intensity)
    {
        if (impulseSource == null) return;
        if (_settings != null && !_settings.ShakeEnabled) return;
        float force = intensity switch
        {
            ShakeIntensity.Small  => smallShake,
            ShakeIntensity.Medium => mediumShake,
            ShakeIntensity.Large  => largeShake,
            _ => mediumShake,
        };
        impulseSource.GenerateImpulse(force);
    }

    public void ShakeCamera(float force = 1f)
    {
        if (impulseSource == null) return;
        impulseSource.GenerateImpulse(force);
    }

    public void AddTargetIfNotExists(Transform target)
    {
        for (int i = 0; i < targetGroup.Targets.Count; i++)
            if (targetGroup.Targets[i].Object == target) return;

        targetGroup.AddMember(target, targetWeight, targetRadius);
    }
}
