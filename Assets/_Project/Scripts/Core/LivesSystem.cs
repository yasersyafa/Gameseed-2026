using System.Collections;
using UnityEngine;
using VContainer;

public class LivesSystem : MonoBehaviour
{
    [Header("Config (override SerializeField if assigned)")]
    [SerializeField] private RoundConfigSO configSO;

    [Header("Settings")]
    [SerializeField] private int   livesPerPlayer = 1;
    [SerializeField] private float respawnDelay   = 2f;

    private RoundManager    _round;
    private IObjectResolver _resolver;

    [Inject]
    public void Construct(IObjectResolver resolver)
    {
        _resolver = resolver;
    }

    private Transform[] _spawnPoints;
    private int[]       _lives;
    private int         _alivePlayers;
    private bool        _roundOver = false;

    private void Awake()
    {
        ApplyConfigFromSO();
    }

    private void Start()
    {
        _round = _resolver?.Resolve<RoundManager>();
    }

    private void ApplyConfigFromSO()
    {
        if (configSO == null) return;
        livesPerPlayer = configSO.livesPerPlayer;
        respawnDelay   = configSO.respawnDelay;
    }

    public void RegisterPlayers(int count, Transform[] spawnPoints)
    {
        _spawnPoints  = spawnPoints;
        _lives        = new int[count];
        _alivePlayers = count;
        _roundOver    = false;

        for (int i = 0; i < count; i++)
            _lives[i] = livesPerPlayer;
    }

    public void PlayerDied(int playerIndex, PlayerController controller)
    {
        if (_roundOver || _lives == null) return;

        GameEvents.RaisePlayerHit(playerIndex, controller);

        _lives[playerIndex]--;

        if (_lives[playerIndex] <= 0)
        {
            _alivePlayers--;
            HidePlayer(controller);
            GameEvents.RaisePlayerEliminated(playerIndex, controller);
            CheckRoundWinCondition();
        }
        else
        {
            StartCoroutine(RespawnRoutine(playerIndex, controller));
        }
    }

    private void HidePlayer(PlayerController controller)
    {
        if (controller.visual != null)
            controller.visual.enabled = false;

        if (controller.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        if (controller.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic    = true;
        }

        controller.SetEliminated(true);
        controller.transform.position = new Vector3(0f, -100f, 0f);
    }

    private void ShowPlayer(PlayerController controller, Vector3 position)
    {
        controller.transform.position = position;

        if (controller.visual != null)
            controller.visual.enabled = true;

        if (controller.TryGetComponent<Collider>(out var col))
            col.enabled = true;

        if (controller.TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = false;

        controller.SetEliminated(false);
        controller.ResetState();
    }

    private IEnumerator RespawnRoutine(int index, PlayerController controller)
    {
        HidePlayer(controller);

        yield return YieldCollection.WaitForSeconds(respawnDelay);

        if (_roundOver || _lives[index] <= 0) yield break;

        Vector3 spawnPos = (_spawnPoints != null && index < _spawnPoints.Length)
            ? _spawnPoints[index].position
            : Vector3.zero;

        ShowPlayer(controller, spawnPos);
        GameEvents.RaisePlayerRespawned(index, controller);
    }

    private void CheckRoundWinCondition()
    {
        if (_alivePlayers > 1) return;

        _roundOver = true;

        for (int i = 0; i < _lives.Length; i++)
        {
            if (_lives[i] > 0)
            {
                _round?.OnRoundEnd(i);
                return;
            }
        }

        _round?.OnRoundEnd(-1);
    }

    public int  GetLives(int playerIndex)
    {
        if (_lives == null || playerIndex >= _lives.Length) return 0;
        return _lives[playerIndex];
    }

    public void ShowPlayerFromRound(PlayerController controller, Vector3 position)
        => ShowPlayer(controller, position);
}