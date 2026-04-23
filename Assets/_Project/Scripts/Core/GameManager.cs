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
    [SerializeField] private int maxPlayers = 2;

    private PlayerInputManager _inputManager;
    private int _playerCount = 0;
    private readonly List<PlayerController> _players = new();
    private bool _gameStarted = false;

    private void Awake()
    {
        Instance      = this;
        _inputManager = GetComponent<PlayerInputManager>();
    }

    private void Start()
    {
        _inputManager.JoinPlayer(
            playerIndex: 0,
            splitScreenIndex: -1,
            controlScheme: "WASD",
            pairWithDevice: Keyboard.current
        );

        _inputManager.JoinPlayer(
            playerIndex: 1,
            splitScreenIndex: -1,
            controlScheme: "ArrowKeys",
            pairWithDevice: Keyboard.current
        );
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

        // Debug.Log($"Player {index + 1} joined!");

        if (_playerCount >= maxPlayers)
            StartGame();
    }

    private void StartGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        
        _inputManager.DisableJoining();
        RoundManager.Instance?.InitRound(_playerCount);

        // Debug.Log("Game Started!");
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