using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("Round Settings")]
    [SerializeField] private int   pointsToWin       = 5;
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float roundEndDelay     = 2f;

    private int[] _scores;
    private int   _playerCount;
    private int   _currentRound    = 0;
    private bool  _roundActive     = false;
    private bool  _isTransitioning = false;

    private void Awake()
    {
        Instance = this;
    }

    public void InitRound(int playerCount)
    {
        if (_isTransitioning) return;

        _isTransitioning = true;
        _playerCount     = playerCount;
        _scores          = new int[playerCount];
        _currentRound    = 0;
        StartCoroutine(StartRoundRoutine());
    }

    private IEnumerator StartRoundRoutine()
    {
        _currentRound++;
        _roundActive     = false;
        _isTransitioning = true;

        yield return null;

        ResetAllPlayers();
        SetAllPlayersFreeze(true);

        for (int i = Mathf.RoundToInt(countdownDuration); i > 0; i--)
        {
            GameEvents.RaiseCountdownTick(i);
            yield return YieldCollection.WaitForSeconds(1f);
        }

        GameEvents.RaiseCountdownTick(0);

        LivesSystem.Instance?.RegisterPlayers(_playerCount,
            GameManager.Instance.GetSpawnPoints());

        SetAllPlayersFreeze(false);
        _roundActive     = true;
        _isTransitioning = false;

        GameEvents.RaiseRoundStarted(_currentRound);
    }

    public void OnRoundEnd(int winnerIndex)
    {
        if (!_roundActive || _isTransitioning) return;

        _isTransitioning = true;
        _roundActive     = false;

        StartCoroutine(RoundEndRoutine(winnerIndex));
    }

    private IEnumerator RoundEndRoutine(int winnerIndex)
    {
        SetAllPlayersFreeze(true);

        if (winnerIndex >= 0 && winnerIndex < _scores.Length)
            _scores[winnerIndex]++;

        GameEvents.RaiseScoresUpdated(_scores);
        GameEvents.RaiseRoundEnded(winnerIndex);

        if (_scores[winnerIndex] >= pointsToWin)
        {
            GameEvents.RaiseGameOver(winnerIndex);
            yield break;
        }

        yield return YieldCollection.WaitForSeconds(roundEndDelay);

        StartCoroutine(StartRoundRoutine());
    }

    private void ResetAllPlayers()
    {
        GameManager.Instance.DisableJoining();

        var players     = GameManager.Instance.GetAllPlayers();
        var spawnPoints = GameManager.Instance.GetSpawnPoints();

        for (int i = 0; i < players.Count; i++)
        {
            Vector3 spawnPos = i < spawnPoints.Length
                ? spawnPoints[i].position
                : Vector3.zero;

            LivesSystem.Instance?.ShowPlayerFromRound(players[i], spawnPos);
            CinemachineCameraManager.Instance?.AddTargetIfNotExists(players[i].transform);
        }
    }

    private void SetAllPlayersFreeze(bool freeze)
    {
        foreach (var player in GameManager.Instance.GetAllPlayers())
            player.SetFreeze(freeze);
    }

    public bool  IsRoundActive()              => _roundActive;
    public int   GetScore(int playerIndex)    => _scores?[playerIndex] ?? 0;
    public int   GetPointsToWin()             => pointsToWin;
    public int   GetCurrentRound()            => _currentRound;
}