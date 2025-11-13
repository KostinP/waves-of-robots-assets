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
        OnPlayersUpdated();
    }

    public void OnPlayerLeft(int connectionId)
    {
        OnPlayersUpdated();
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД
    public void OnPlayersUpdated()
    {
        _mainMenuController?.UpdatePlayerList();
        _hudController?.UpdatePlayerList();
    }

    public void OnLobbyListUpdated()
    {
        _mainMenuController?.OnLobbyListUpdated();
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД
    public void OnLobbyCreated()
    {
        _mainMenuController?.OnLobbyCreated();
    }

    // ДОБАВЬТЕ ЭТОТ МЕТОД
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
        Debug.Log("UIManager: Leaving game...");

        try
        {
            // Останавливаем все сетевые активности
            var lobbyManager = FindObjectOfType<LobbyManager>();
            if (lobbyManager != null)
            {
                // ФИКС: Используем DisbandLobby вместо прямого вызова
                lobbyManager.DisbandLobby();
            }
            else
            {
                Debug.LogWarning("LobbyManager not found, proceeding without disband");
                // Если менеджера нет, просто загружаем меню
                LoadMainMenuDirectly();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Error leaving game: {e.Message}");
            LoadMainMenuDirectly();
        }
    }

    private void LoadMainMenuDirectly()
    {
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
        else
        {
            SceneManager.LoadScene(0); // Первая сцена
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
            // Используем _mainMenuController
            if (_mainMenuController != null)
            {
                _mainMenuController.ReturnToLobbyList();
            }
            else
            {
                // Если контроллер не найден, пытаемся найти его
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

        // Принудительный запрос discovery
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.ForceDiscovery();
        }
    }


    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}