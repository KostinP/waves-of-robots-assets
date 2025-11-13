using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using UnityEngine.InputSystem;
using System;
using System.Collections;

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
        Debug.Log($"UIManager: Scene loaded: {scene.name}");

        if (scene.name == "MainMenuScene" || scene.name == "MainMenu")
        {
            InitializeMainMenuUI();
        }
        else if (scene.name == "GameCoreScene" || scene.name == "Game")
        {
            InitializeGameUI();
        }
    }

    private void InitializeMainMenuUI()
    {
        _mainMenuController = FindObjectOfType<MainMenuController>();
        if (_mainMenuController == null)
        {
            Debug.LogWarning("MainMenuController not found in scene, creating new one");
            // Создаем через префаб или оставляем поиск в следующем кадре
            StartCoroutine(FindMainMenuControllerDelayed());
        }
    }

    private void InitializeGameUI()
    {
        _hudController = FindObjectOfType<HUDController>();
        _pauseController = FindObjectOfType<PauseMenuController>();
        
        // Если контроллеры не найдены, они будут созданы SceneUIController
    }

    private IEnumerator FindMainMenuControllerDelayed()
    {
        yield return new WaitForEndOfFrame();
        _mainMenuController = FindObjectOfType<MainMenuController>();
        if (_mainMenuController == null)
        {
            Debug.LogError("MainMenuController still not found after delay");
        }
    }

    // === СОХРАНЯЕМ ВСЮ СУЩЕСТВУЮЩУЮ ФУНКЦИОНАЛЬНОСТЬ ===

    public void SetHUDController(HUDController controller) => _hudController = controller;
    public void SetPauseController(PauseMenuController controller) => _pauseController = controller;

    public void OnPlayerJoined(int connectionId, string name)
    {
        OnPlayersUpdated();
    }

    public void OnPlayerLeft(int connectionId)
    {
        OnPlayersUpdated();
    }

    public void OnPlayersUpdated()
    {
        _mainMenuController?.UpdatePlayerList();
        _hudController?.UpdatePlayerList();
    }

    public void OnLobbyListUpdated()
    {
        _mainMenuController?.OnLobbyListUpdated();
    }

    public void OnLobbyCreated()
    {
        _mainMenuController?.OnLobbyCreated();
    }

    public void OnJoinedAsClient()
    {
        _mainMenuController?.OnJoinedAsClient();
    }

    public void StartGame()
    {
        LobbyManager.StartGame();
        SceneManager.LoadScene("GameCoreScene");
    }

    public void LeaveGame()
    {
        try
        {
            var lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager != null)
            {
                lobbyManager.DisbandLobby();
            }
            else
            {
                Debug.LogWarning("LobbyManager not found, proceeding without disband");
                LoadMainMenuDirectly();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error leaving game: {e.Message}");
            LoadMainMenuDirectly();
        }
    }

    public string GetCurrentScreen()
    {
        var mainMenuController = FindObjectOfType<MainMenuController>();
        return mainMenuController?.GetCurrentScreen() ?? "unknown";
    }

    private void LoadMainMenuDirectly()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenuScene"))
        {
            SceneManager.LoadScene("MainMenuScene");
        }
        else if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void ShowMainMenuScreen()
    {
        _mainMenuController?.ShowScreen(UIScreenManager.MenuScreenName);
    }

    public void ShowLobbyListScreen()
    {
        _mainMenuController?.ShowScreen(UIScreenManager.LobbyListScreenName);
        OnLobbyListUpdated();
    }

    public void ReturnToLobbyList()
    {
        Debug.Log("UIManager: Returning to lobby list");

        try
        {
            if (_mainMenuController != null)
            {
                _mainMenuController.ReturnToLobbyList();
            }
            else
            {
                _mainMenuController = FindObjectOfType<MainMenuController>();
                _mainMenuController?.ReturnToLobbyList();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error in ReturnToLobbyList: {e.Message}");
        }
    }

    private IEnumerator DelayedLobbyUpdate()
    {
        yield return new WaitForSeconds(0.5f);
        OnLobbyListUpdated();

        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.ForceDiscovery();
        }
    }

    public void ShowPauseMenu()
    {
        _pauseController?.ShowMenu();
    }

    public void HidePauseMenu()
    {
        _pauseController?.HideMenu();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}