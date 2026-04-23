using System;
using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    [SerializeField] private int   pointsToWin    = 5;    // first to X points
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float roundEndDelay  = 2f;   // jeda sebelum round baru

    // Score per player — index = playerIndex
    private int[] _scores;
    private int _playerCount;
    private int _currentRound = 0;
    private bool _roundActive  = false;
    private bool _isTransitioning = false;

    // Event — UI akan subscribe ke ini nanti di Phase UI
    public event Action<int[]> OnScoresUpdated;   // scores array
    public event Action<int> OnCountdown;       // angka countdown
    public event Action<int> OnRoundStarted;    // round number
    public event Action<int> OnRoundEnded;      // winner index
    public event Action<int> OnGameOver;        // overall winner index

    private void Awake()
    {
        Instance = this;
    }

    // Dipanggil GameManager setelah semua player join
    public void InitRound(int playerCount)
    {
        if (_isTransitioning) return;

        _isTransitioning = true;
        _playerCount  = playerCount;
        _scores       = new int[playerCount];
        _currentRound = 0;
        StartCoroutine(StartRoundRoutine());
    }

    private IEnumerator StartRoundRoutine()
    {
        _currentRound++;
        _roundActive = false;
        _isTransitioning = true;

        yield return null;

        Debug.Log($"=== Round {_currentRound} ===");

        // Reset semua player ke spawn point
        ResetAllPlayers();

        // Freeze semua player selama countdown
        SetAllPlayersFreeze(true);

        // Countdown 3-2-1
        for (int i = Mathf.RoundToInt(countdownDuration); i > 0; i--)
        {
            OnCountdown?.Invoke(i);
            Debug.Log($"Countdown: {i}");
            yield return  YieldCollection.WaitForSeconds(1f);
        }

        // GO!
        OnCountdown?.Invoke(0);
        OnRoundStarted?.Invoke(_currentRound);

        // Register nyawa ke LivesSystem — round baru = nyawa reset
        LivesSystem.Instance?.RegisterPlayers(_playerCount,
            GameManager.Instance.GetSpawnPoints());

        // Unfreeze semua player
        SetAllPlayersFreeze(false);
        _roundActive = true;
        _isTransitioning = false;

        Debug.Log("Round started!");
    }

    // Dipanggil LivesSystem saat tinggal 1 player tersisa
    public void OnRoundEnd(int winnerIndex)
    {
        if (!_roundActive) return;
        if (_isTransitioning) return;
        _isTransitioning = true;
        _roundActive = false;

        StartCoroutine(RoundEndRoutine(winnerIndex));
    }

    private IEnumerator RoundEndRoutine(int winnerIndex)
    {
        // Freeze semua player saat round selesai
        SetAllPlayersFreeze(true);

        // Tambah poin ke winner
        if (winnerIndex >= 0 && winnerIndex < _scores.Length)
            _scores[winnerIndex]++;

        OnScoresUpdated?.Invoke(_scores);
        OnRoundEnded?.Invoke(winnerIndex);

        Debug.Log($"Player {winnerIndex + 1} menang round! Score: {_scores[winnerIndex]}/{pointsToWin}");

        // Cek apakah ada yang menang keseluruhan
        if (_scores[winnerIndex] >= pointsToWin)
        {
            OnGameOver?.Invoke(winnerIndex);
            Debug.Log($"=== Player {winnerIndex + 1} MENANG GAME! ===");
            yield break;
        }

        yield return YieldCollection.WaitForSeconds(roundEndDelay);

        // Mulai round baru
        StartCoroutine(StartRoundRoutine());
    }

    private void ResetAllPlayers()
    {
        GameManager.Instance.DisableJoining();

        var players     = GameManager.Instance.GetAllPlayers();
        var spawnPoints = GameManager.Instance.GetSpawnPoints();

        for (int i = 0; i < players.Count; i++)
        {
            var player = players[i];

            // Tidak ada SetActive sama sekali — pakai ShowPlayer
            Vector3 spawnPos = i < spawnPoints.Length
                ? spawnPoints[i].position
                : Vector3.zero;

            LivesSystem.Instance?.ShowPlayerFromRound(player, spawnPos);
            CinemachineCameraManager.Instance?.AddTargetIfNotExists(player.transform);
        }
    }

    private void SetAllPlayersFreeze(bool freeze)
    {
        var players = GameManager.Instance.GetAllPlayers();
        foreach (var player in players)
            player.SetFreeze(freeze);
    }

    public bool IsRoundActive() => _roundActive;
    public int  GetScore(int playerIndex) => _scores?[playerIndex] ?? 0;
    public int  GetPointsToWin() => pointsToWin;
    public int  GetCurrentRound() => _currentRound;
}