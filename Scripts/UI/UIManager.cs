using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [SerializeField] private InputActionAsset inputActions;
    public InputActionAsset InputActions => inputActions;

    public LobbyManager LobbyManager { get; private set; }

    private MainMenuController _mainMenuController;
    private HUDController _hudController;
    private PauseMenuController _pauseController;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LobbyManager = GetComponent<LobbyManager>() ?? gameObject.AddComponent<LobbyManager>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "MainMenuScene")
        {
            _mainMenuController = FindObjectOfType<MainMenuController>();
        }
        else if (scene.name == "GameCoreScene")
        {
            // HUD и Pause загружаются в SceneUIController
        }
    }

    public void SetHUDController(HUDController controller) => _hudController = controller;
    public void SetPauseController(PauseMenuController controller) => _pauseController = controller;

    // === События для UI ===
    public void OnPlayerJoined(int connectionId, string name)
    {
        _mainMenuController?.UpdatePlayerList();
        _hudController?.UpdatePlayerList();
    }

    public void OnPlayerLeft(int connectionId)
    {
        _mainMenuController?.UpdatePlayerList();
        _hudController?.UpdatePlayerList();
    }

    public void OnLobbyListUpdated()
    {
        _mainMenuController?.OnLobbyListUpdated();
    }

    public void StartGame()
    {
        LobbyManager.StartGame();
        SceneManager.LoadScene("GameCoreScene");
    }

    public void LeaveGame()
    {
        LobbyManager.DisbandLobby();
        SceneManager.LoadScene("ManagersScene");
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}