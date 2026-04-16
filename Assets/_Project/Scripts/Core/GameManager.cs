using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInputManager))]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player Settings")]
    [SerializeField] private Transform[] spawnPoints;   // 4 titik spawn di arena
    [SerializeField] private Material[] playerMaterials; // 4 warna berbeda

    private PlayerInputManager _inputManager;
    private int _playerCount = 0;

    private void Awake()
    {
        Instance = this;
        _inputManager = GetComponent<PlayerInputManager>();
    }

    // Dipanggil otomatis oleh PlayerInputManager saat player baru join
    private void OnPlayerJoined(PlayerInput playerInput)
    {
        int index = _playerCount;
        _playerCount++;

        // Spawn di titik yang sesuai
        if (index < spawnPoints.Length)
            playerInput.transform.position = spawnPoints[index].position;

        // Warna berbeda per player
        if (index < playerMaterials.Length)
        {
            var renderer = playerInput.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material = playerMaterials[index];
        }

        // Kasih tau PlayerController index-nya
        playerInput.GetComponent<PlayerController>().Init(index);
        CinemachineCameraManager.Instance?.AddTarget(playerInput.transform);

        Debug.Log($"Player {index + 1} joined!");
    }
}