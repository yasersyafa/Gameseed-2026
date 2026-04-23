using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LivesSystem : MonoBehaviour
{
    public static LivesSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int   livesPerPlayer = 1;
    [SerializeField] private float respawnDelay   = 2f;

    private Transform[] _spawnPoints;
    private int[] _lives;
    private int _alivePlayers;
    private bool _roundOver = false;

    private void Awake()
    {
        Instance = this;
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
        if (_roundOver) return;
        if (_lives == null) return;

        _lives[playerIndex]--;

        if (_lives[playerIndex] <= 0)
        {
            _alivePlayers--;

            // JANGAN SetActive(false) — pakai HidePlayer instead
            HidePlayer(controller);
            CinemachineCameraManager.Instance?.RemoveTarget(controller.transform);
            CheckRoundWinCondition();
        }
        else
        {
            StartCoroutine(RespawnRoutine(playerIndex, controller));
        }
    }

    private void HidePlayer(PlayerController controller)
    {
        // Disable visual
        if (controller.visual != null)
            controller.visual.enabled = false;

        // Disable collider
        if (controller.TryGetComponent<Collider>(out var col))
            col.enabled = false;

        // Freeze rigidbody
        if (controller.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic    = true;
        }

        // Tandai sebagai eliminated
        controller.SetEliminated(true);

        // Pindah ke posisi tersembunyi jauh dari arena
        controller.transform.position = new Vector3(0f, -100f, 0f);
    }

    private void ShowPlayer(PlayerController controller, Vector3 position)
    {
        // Pindah ke spawn point dulu sebelum di-show
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

    // private IEnumerator RespawnRoutine(int index, PlayerController controller)
    // {
    //     var rb   = controller.GetComponent<Rigidbody>();
    //     var col  = controller.GetComponent<Collider>();
    //     var rend = controller.visual;

    //     if (rb)   rb.isKinematic = true;
    //     if (col)  col.enabled    = false;
    //     if (rend) rend.enabled   = false;
    //     controller.enabled       = false;

    //     yield return YieldCollection.WaitForSeconds(respawnDelay);

    //     if (_roundOver || _lives[index] <= 0) yield break;

    //     if (_spawnPoints != null && index < _spawnPoints.Length)
    //         controller.transform.position = _spawnPoints[index].position;

    //     if (rb)   rb.isKinematic = false;
    //     if (col)  col.enabled    = true;
    //     if (rend) rend.enabled   = true;
    //     controller.enabled       = true;
    //     controller.ResetState();
    // }

    private IEnumerator RespawnRoutine(int index, PlayerController controller)
    {
        HidePlayer(controller);

        yield return YieldCollection.WaitForSeconds(respawnDelay);

        if (_roundOver || _lives[index] <= 0) yield break;

        Vector3 spawnPos = (_spawnPoints != null && index < _spawnPoints.Length)
            ? _spawnPoints[index].position
            : Vector3.zero;

        ShowPlayer(controller, spawnPos);
    }

    private void CheckRoundWinCondition()
    {
        if (_alivePlayers > 1) return;

        _roundOver = true;

        var allPlayers = GameManager.Instance.GetAllPlayers();
        for (int i = 0; i < allPlayers.Count; i++)
        {
            if (_lives[i] > 0)
            {
                RoundManager.Instance?.OnRoundEnd(i); // CHANGED
                return;
            }
        }

        // Edge case: draw
        RoundManager.Instance?.OnRoundEnd(-1);
    }

    public int GetLives(int playerIndex)
    {
        if (_lives == null || playerIndex >= _lives.Length) return 0;
        return _lives[playerIndex];
    }

    public void ShowPlayerFromRound(PlayerController controller, Vector3 position)
    {
        ShowPlayer(controller, position);
    }
}