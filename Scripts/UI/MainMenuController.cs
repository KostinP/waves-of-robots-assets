using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    private VisualElement _root;
    private UIInputManager _inputManager;
    private UIScreenManager _screenManager;
    private UILobbySetupManager _lobbySetup;
    private UICharacterSelectionManager _charSelect;
    private UISettingsManager _settings;
    private LobbySettingsManager _lobbySettings;

    public UILobbySetupManager LobbySetupManager => _lobbySetup;
    public UICharacterSelectionManager CharacterSelectionManager => _charSelect;

    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;

        _inputManager = new UIInputManager(UIManager.Instance.InputActions, _root, this);

        _screenManager = new UIScreenManager(_root, this);  // ← this!
        _lobbySetup = new UILobbySetupManager(_root, this);
        _charSelect = new UICharacterSelectionManager(_root);
        _settings = new UISettingsManager(_root, uiDoc, this);
        _lobbySettings = new LobbySettingsManager(_root);
        _inputManager.Enable();
        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RefreshForNewScene();
            LocalizationManager.Instance.UpdateAllUIElements();
        }
    }

    public void ShowScreen(string screenName)
    {
        _screenManager.ShowScreen(screenName);

        // ПРИНУДИТЕЛЬНОЕ ОБНОВЛЕНИЕ ПРИ ПОКАЗЕ СПИСКА ЛОББИ
        if (screenName == UIScreenManager.LobbyListScreenName)
        {
            // Даем кадр на отрисовку, затем обновляем
            StartCoroutine(DelayedLobbyRefresh());
        }
    }

    private IEnumerator DelayedLobbyRefresh()
    {
        yield return new WaitForEndOfFrame();
        UIManager.Instance.OnLobbyListUpdated();

        // Принудительный запрос discovery
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.ForceDiscovery();
        }
    }

    public void UpdatePlayerList()
    {
        Debug.Log("MainMenuController: UpdatePlayerList called");
        _screenManager.UpdatePlayerList();
    }

    public void OnLobbyCreated()
    {
        Debug.Log("MainMenuController: OnLobbyCreated called");
        ShowScreen(UIScreenManager.LobbySettingsScreenName);
        _lobbySettings.SyncAllSettings();
        UIManager.Instance.OnPlayersUpdated(); // Добавьте эту строку
    }

    public void OnLobbyListUpdated() => _screenManager.RefreshLobbyList();

    public void OnJoinedAsClient()
    {
        ShowScreen("lobby_settings_screen");
        SetupClientModeUI();
    }

    public void SetupHostModeUI()
    {
        var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
        if (lobbySettingsScreen == null) return;

        // Показываем все элементы для хоста
        var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
        var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
        var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
        var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

        if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.Flex;
        if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.Flex;
        if (lobbyNameField != null) lobbyNameField.SetEnabled(true);
        if (playerCountSlider != null) playerCountSlider.SetEnabled(true);

        Debug.Log("UI set to host mode - all controls enabled");
    }

    public void ReturnToLobbyList()
    {
        Debug.Log("MainMenuController: Returning to lobby list");
        ShowScreen(UIScreenManager.LobbyListScreenName);

        // Принудительно обновляем список лобби
        StartCoroutine(DelayedLobbyRefresh2());
    }

    private IEnumerator DelayedLobbyRefresh2()
    {
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance?.OnLobbyListUpdated();
        
        // Принудительный запрос discovery
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.ForceDiscovery();
        }
    }

    public void HandleLobbyClosed(string lobbyId)
    {
        Debug.Log($"MainMenuController: Handling lobby close for {lobbyId}");
        StartCoroutine(HandleLobbyClosedCoroutine(lobbyId));
    }

    private IEnumerator HandleLobbyClosedCoroutine(string lobbyId)
    {
        yield return new WaitForSeconds(0.5f);

        // Если мы находимся в настройках лобби, возвращаемся к списку
        if (_screenManager.GetCurrentScreen() == UIScreenManager.LobbySettingsScreenName)
        {
            Debug.Log("We were in the closed lobby, returning to lobby list");
            ReturnToLobbyList();
        }
    }

    public void ShowLobbyListAfterDisband()
    {
        StartCoroutine(ShowLobbyListAfterDisbandCoroutine());
    }

    private IEnumerator ShowLobbyListAfterDisbandCoroutine()
    {
        yield return new WaitForSeconds(1f); // Даем время на завершение сетевых операций

        ShowScreen(UIScreenManager.LobbyListScreenName);

        // Принудительно обновляем список лобби
        UIManager.Instance?.OnLobbyListUpdated();

        // Принудительный запрос discovery
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.ForceDiscovery();
        }
    }

    private void SetupClientModeUI()
    {
        var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
        if (lobbySettingsScreen == null) return;

        // Скрываем элементы, которые недоступны клиентам
        var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
        var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
        var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
        var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

        if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.None;
        if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.None;
        if (lobbyNameField != null) lobbyNameField.SetEnabled(false);
        if (playerCountSlider != null) playerCountSlider.SetEnabled(false);

        Debug.Log("UI set to client mode - host controls disabled");
    }

    private void OnDestroy()
    {
        _inputManager?.Cleanup();
        _settings?.Cleanup();
    }
}