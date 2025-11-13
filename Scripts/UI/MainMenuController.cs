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
        _screenManager = new UIScreenManager(_root, this);
        _lobbySetup = new UILobbySetupManager(_root, this);
        _charSelect = new UICharacterSelectionManager(_root);
        _settings = new UISettingsManager(_root, uiDoc, this);
        _lobbySettings = new LobbySettingsManager(_root);
        _inputManager.Enable();
        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);

        LocalizationManager.Instance?.RefreshForNewScene();
        LocalizationManager.Instance?.UpdateAllUIElements();
    }

    public void ShowScreen(string screenName)
    {
        _screenManager.ShowScreen(screenName);

        if (screenName == UIScreenManager.LobbyListScreenName)
        {
            StartCoroutine(DelayedLobbyRefresh());
        }
    }

    private IEnumerator DelayedLobbyRefresh()
    {
        yield return new WaitForEndOfFrame();
        UIManager.Instance.OnLobbyListUpdated();

        LobbyDiscovery.Instance?.ForceDiscovery();
    }

    public void UpdatePlayerList()
    {
        _screenManager.UpdatePlayerList();
    }

    public void OnLobbyCreated()
    {
        ShowScreen(UIScreenManager.LobbySettingsScreenName);
        _lobbySettings.SyncAllSettings();
        UIManager.Instance.OnPlayersUpdated();
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
        StartCoroutine(DelayedLobbyRefresh2());
    }

    private IEnumerator DelayedLobbyRefresh2()
    {
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance?.OnLobbyListUpdated();
        LobbyDiscovery.Instance?.ForceDiscovery();
    }

    public void HandleLobbyClosed(string lobbyId)
    {
        Debug.Log($"MainMenuController: Handling lobby close for {lobbyId}, current screen: {GetCurrentScreen()}");
        if (GetCurrentScreen() == UIScreenManager.LobbySettingsScreenName)
        {
            Debug.Log("We were in the closed lobby, returning to lobby list");
            StartCoroutine(HandleLobbyClosedCoroutine(lobbyId));
        }
        else
        {
            Debug.Log($"Not returning to lobby list because we're on screen: {GetCurrentScreen()}");
        }
    }

    private IEnumerator HandleLobbyClosedCoroutine(string lobbyId)
    {
        yield return new WaitForSeconds(0.5f);
        ReturnToLobbyList();
    }

    public string GetCurrentScreen()
    {
        return _screenManager?.GetCurrentScreen() ?? "unknown";
    }

    public void ShowLobbyListAfterDisband()
    {
        StartCoroutine(ShowLobbyListAfterDisbandCoroutine());
    }

    private IEnumerator ShowLobbyListAfterDisbandCoroutine()
    {
        yield return new WaitForSeconds(1f);
        ShowScreen(UIScreenManager.LobbyListScreenName);
        UIManager.Instance?.OnLobbyListUpdated();
        LobbyDiscovery.Instance?.ForceDiscovery();
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