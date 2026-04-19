using UnityEngine;
using System.Collections;

public class LivesSystem : MonoBehaviour
{
    public static LivesSystem Instance { get; private set; }

    [SerializeField] private int livesPerPlayer = 3;
    [SerializeField] private float respawnDelay = 2f;
    [SerializeField] private Transform[] spawnPoints;

    private int[] _lives;
    private int _alivePlayers;

    private void Awake() => Instance = this;

    public void RegisterPlayers(int count)
    {
        _lives        = new int[count];
        _alivePlayers = count;
        for (int i = 0; i < count; i++) _lives[i] = livesPerPlayer;
    }

    public void PlayerDied(int playerIndex, PlayerController controller)
    {
        // Guard: jika RegisterPlayers belum dipanggil
        if (_lives == null || playerIndex >= _lives.Length) return;

        _lives[playerIndex]--;
        Debug.Log($"Player {playerIndex + 1} lives: {_lives[playerIndex]}");

        if (_lives[playerIndex] <= 0)
        {
            _alivePlayers--;
            controller.gameObject.SetActive(false);
            CinemachineCameraManager.Instance?.RemoveTarget(controller.transform);
            CheckWinCondition();
        }
        else
        {
            StartCoroutine(RespawnRoutine(playerIndex, controller));
        }
    }

    private IEnumerator RespawnRoutine(int index, PlayerController controller)
    {
        controller.gameObject.SetActive(false);
        yield return new WaitForSeconds(respawnDelay);

        if (index < spawnPoints.Length)
            controller.transform.position = spawnPoints[index].position;

        controller.gameObject.SetActive(true);
        controller.ResetState();
    }

    private void CheckWinCondition()
    {
        if (_alivePlayers <= 1)
        {
            var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
            foreach (var p in allPlayers)
            {
                if (p.gameObject.activeSelf)
                {
                    Debug.Log($"{p.name} MENANG!");
                    break;
                }
            }
        }
    }
}