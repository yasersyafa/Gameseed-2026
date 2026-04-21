using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LivesSystem : MonoBehaviour
{
    public static LivesSystem Instance { get; private set; }

    [Header("Settings")]
    [SerializeField] private int   livesPerPlayer = 3;
    [SerializeField] private float respawnDelay   = 2f;

    // Spawn points diambil dari GameManager supaya tidak duplikat referensi
    private Transform[] _spawnPoints;
    private int[]       _lives;
    private int         _alivePlayers;
    private bool        _gameOver = false;

    private void Awake()
    {
        Instance = this;
    }

    public void RegisterPlayers(int count, Transform[] spawnPoints)
    {
        _spawnPoints  = spawnPoints;
        _lives        = new int[count];
        _alivePlayers = count;
        _gameOver     = false;

        for (int i = 0; i < count; i++)
            _lives[i] = livesPerPlayer;

        // Debug.Log($"LivesSystem: {count} player, masing-masing {livesPerPlayer} nyawa.");
    }

    // Dipanggil PlayerController.OnHitByBoomerang()
    public void PlayerDied(int playerIndex, PlayerController controller)
    {
        if (_gameOver) return;
        if (_lives == null)
        {
            // Debug.LogWarning("LivesSystem: RegisterPlayers belum dipanggil!");
            return;
        }

        _lives[playerIndex]--;
        // Debug.Log($"Player {playerIndex + 1} mati! Sisa nyawa: {_lives[playerIndex]}");

        if (_lives[playerIndex] <= 0)
        {
            // Nyawa habis → eliminated permanen
            _alivePlayers--;
            controller.gameObject.SetActive(false);
            CinemachineCameraManager.Instance?.RemoveTarget(controller.transform);
            CheckWinCondition();
        }
        else
        {
            // Masih ada nyawa → respawn object yang sama, BUKAN spawn baru
            StartCoroutine(RespawnRoutine(playerIndex, controller));
        }
    }

    private IEnumerator RespawnRoutine(int index, PlayerController controller)
    {
        var rb   = controller.GetComponent<Rigidbody>();
        var col  = controller.GetComponent<Collider>();
        var rend = controller.visual;

        if (rb)   rb.isKinematic = true;
        if (col)  col.enabled    = false;
        if (rend) rend.enabled   = false;
        controller.enabled       = false;

        yield return YieldCollection.WaitForSeconds(respawnDelay);


        if (_gameOver || _lives[index] <= 0) yield break;

        if (_spawnPoints != null && index < _spawnPoints.Length)
            controller.transform.position = _spawnPoints[index].position;

        if (rb)   rb.isKinematic = false;
        if (col)  col.enabled    = true;
        if (rend) rend.enabled   = true;
        controller.enabled       = true;
        controller.ResetState();

        // Debug.Log($"Player {index + 1} respawn! Sisa nyawa: {_lives[index]}");
    }

    private void CheckWinCondition()
    {
        if (_alivePlayers > 1) return;

        _gameOver = true;

        // Cari player yang masih hidup
        var allPlayers = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in allPlayers)
        {
            if (p.gameObject.activeSelf)
            {
                // Debug.Log($"=== {p.name} MENANG! ===");
                break;
            }
        }

        // Nanti di Phase 4: tampilkan UI menang di sini
    }

    public int GetLives(int playerIndex)
    {
        if (_lives == null || playerIndex >= _lives.Length) return 0;
        return _lives[playerIndex];
    }
}