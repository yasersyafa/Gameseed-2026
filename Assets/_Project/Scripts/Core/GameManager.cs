using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private Material[]  playerMaterials;

    [Header("Game Settings")]
    [SerializeField] private int  maxPlayers       = 4;
    [SerializeField] private int  minPlayersToStart = 2;

    [Header("Auto-Join (debug / quick test)")]
    [SerializeField] private bool autoJoinKeyboard1 = true;   // WASD slot
    [SerializeField] private bool autoJoinKeyboard2 = true;   // ArrowKeys slot
    [SerializeField] private float autoStartDelay   = 5f;     // detik tunggu sebelum start kalau lobby aktif

    private PlayerInputManager _inputManager;
    private int _playerCount = 0;
    private readonly List<PlayerController> _players = new();
    private bool _gameStarted = false;

    public int MaxPlayers => maxPlayers;
    public int PlayerCount => _playerCount;

    private void Awake()
    {
        Instance      = this;
        _inputManager = GetComponent<PlayerInputManager>();

        // PlayerInputManager.maxPlayerCount is read-only API.
        // Set "Max Player Count" di Inspector (PlayerInputManager component) =
        // sama dengan maxPlayers di sini.
#if UNITY_EDITOR
        if (_inputManager != null && _inputManager.maxPlayerCount != maxPlayers)
            Debug.LogWarning(
                $"[GameManager] PlayerInputManager.maxPlayerCount ({_inputManager.maxPlayerCount}) " +
                $"!= GameManager.maxPlayers ({maxPlayers}). Set keduanya sama di Inspector."
            );
#endif
    }

    private void Start()
    {
        if (autoJoinKeyboard1)
        {
            _inputManager.JoinPlayer(
                playerIndex: 0,
                splitScreenIndex: -1,
                controlScheme: "WASD",
                pairWithDevice: Keyboard.current
            );
        }

        if (autoJoinKeyboard2)
        {
            _inputManager.JoinPlayer(
                playerIndex: 1,
                splitScreenIndex: -1,
                controlScheme: "ArrowKeys",
                pairWithDevice: Keyboard.current
            );
        }

        // Sisanya bisa join lewat lobby (gamepad press, dll)
        // PlayerInputManager handle auto-join kalau Join Behavior = JoinPlayersWhenButtonIsPressed
#if UNITY_EDITOR
        Debug.Log($"[GameManager] Boot maxPlayers={maxPlayers} autoKB1={autoJoinKeyboard1} autoKB2={autoJoinKeyboard2}");
#endif
    }

    /// <summary>
    /// Managing OnPlayerJoined
    /// Run in Player Input Manager
    /// </summary>
    /// <param name="playerInput"></param>
    private void OnPlayerJoined(PlayerInput playerInput)
    {
        int index = _playerCount;
        _playerCount++;

        if (index < spawnPoints.Length)
            playerInput.transform.position = spawnPoints[index].position;

        if (index < playerMaterials.Length)
        {
            var renderer = playerInput.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material = playerMaterials[index];
        }

        var controller = playerInput.GetComponent<PlayerController>();
        controller.Init(index);
        _players.Add(controller);
        CinemachineCameraManager.Instance?.AddTarget(playerInput.transform);

#if UNITY_EDITOR
        Debug.Log($"[GameManager] Player {index} joined ({_playerCount}/{maxPlayers})");
#endif

        // Start otomatis kalau penuh; kalau cukup minimum, kasih jeda lobby
        if (_playerCount >= maxPlayers)
        {
            StartGame();
        }
        else if (_playerCount >= minPlayersToStart && !_gameStarted)
        {
            CancelInvoke(nameof(StartGame));
            Invoke(nameof(StartGame), autoStartDelay);
        }
    }

    private void StartGame()
    {
        if (_gameStarted) return;
        if (_playerCount < minPlayersToStart) return;

        _gameStarted = true;
        _inputManager.DisableJoining();
        RoundManager.Instance?.InitRound(_playerCount);

#if UNITY_EDITOR
        Debug.Log($"[GameManager] StartGame with {_playerCount} players");
#endif
    }

    public List<PlayerController> GetAllPlayers()  => _players;
    public Transform[] GetSpawnPoints() => spawnPoints;
    public void DisableJoining()
    {
        _inputManager.DisableJoining();
    }

    public void EnableJoining()
    {
        _inputManager.EnableJoining();
    }
}