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
    // FIX BUG 3: Tentukan max player di Inspector (sesuaikan dengan jumlah controller)
    [SerializeField] private int maxPlayers = 2;

    private PlayerInputManager _inputManager;
    private int _playerCount = 0;

    private void Awake()
    {
        Instance      = this;
        _inputManager = GetComponent<PlayerInputManager>();
    }

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

        playerInput.GetComponent<PlayerController>().Init(index);
        CinemachineCameraManager.Instance?.AddTarget(playerInput.transform);

        Debug.Log($"Player {index + 1} joined!");

        // FIX BUG 3: RegisterPlayers TIDAK dipanggil di sini lagi.
        // RegisterPlayers sekarang hanya dipanggil sekali saat semua player sudah join,
        // sehingga array nyawa tidak direset setiap kali ada player yang join.
        if (_playerCount >= maxPlayers)
            StartGame();
    }

    private void StartGame()
    {
        // FIX BUG 3: Disable joining setelah semua player join.
        // Ini mencegah PlayerInputManager salah menginterpretasi
        // SetActive(true) saat respawn sebagai player baru yang join.
        _inputManager.DisableJoining();

        // FIX BUG 3: Baru daftarkan player ke LivesSystem sekarang,
        // hanya sekali, setelah jumlah player final sudah diketahui.
        LivesSystem.Instance?.RegisterPlayers(_playerCount);

        Debug.Log("Game Started!");
    }
}