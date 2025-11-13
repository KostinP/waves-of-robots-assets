using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

public class MainMenuController : MonoBehaviour
{
    private UIDocument _uiDocument;
    private VisualElement _root;

    private UIInputManager _inputManager;
    private UIScreenManager _screenManager;
    private UILobbySetupManager _lobbySetup;
    private UICharacterSelectionManager _charSelect;
    private LobbySettingsManager _lobbySettings;
    private UISettingsManager _settingsManager;

    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset mainMenuUXML;
    [SerializeField] private VisualTreeAsset settingsMenuUXML;

    private bool _isInSettingsMode = false;

    public UILobbySetupManager LobbySetupManager => _lobbySetup;
    public UICharacterSelectionManager CharacterSelectionManager => _charSelect;

    private void Start()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument not found on MainMenuController!");
            return;
        }

        // Загружаем основной UXML
        if (mainMenuUXML != null)
        {
            _uiDocument.visualTreeAsset = mainMenuUXML;
        }

        _root = _uiDocument.rootVisualElement;
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        yield return new WaitForEndOfFrame();

        InitializeMainMenuUI();
        Debug.Log("MainMenuController: Инициализация завершена");
    }

    private void InitializeMainMenuUI()
    {
        // Инициализируем НЕ-MonoBehaviour классы для главного меню
        _inputManager = new UIInputManager(UIManager.Instance.InputActions, _root, this);
        _screenManager = new UIScreenManager(_root, this);
        _lobbySetup = new UILobbySetupManager(_root, this);
        _charSelect = new UICharacterSelectionManager(_root);
        _lobbySettings = new LobbySettingsManager(_root);

        _inputManager.Enable();
        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);

        LocalizationManager.Instance?.RefreshForNewScene();
        LocalizationManager.Instance?.UpdateAllUIElements();

        _isInSettingsMode = false;
    }

    private void InitializeSettingsUI()
    {
        // Инициализируем менеджер настроек
        _settingsManager = new UISettingsManager(_root, _uiDocument, this);

        // Настраиваем обработчики для кнопок настроек
        var btnSave = _root.Q<Button>("btnSave");
        var btnCancel = _root.Q<Button>("cancelBtn");

        if (btnSave != null)
        {
            btnSave.clicked += OnSaveSettings;
        }

        if (btnCancel != null)
        {
            btnCancel.clicked += OnCancelSettings;
        }

        _isInSettingsMode = true;
    }

    public void ShowScreen(string screenName)
    {
        if (_isInSettingsMode && screenName != UIScreenManager.SettingsScreenName)
        {
            // Если мы в режиме настроек и пытаемся показать другой экран - сначала скрываем настройки
            HideSettingsScreen();
            return;
        }

        if (screenName == UIScreenManager.SettingsScreenName)
        {
            ShowSettingsScreen();
        }
        else
        {
            _screenManager?.ShowScreen(screenName);
        }

        if (screenName == UIScreenManager.LobbyListScreenName)
        {
            StartCoroutine(DelayedLobbyRefresh());
        }
    }

    private void ShowSettingsScreen()
    {
        if (settingsMenuUXML == null)
        {
            Debug.LogError("SettingsMenu UXML not assigned!");
            return;
        }

        // Переключаем на UXML настроек
        _uiDocument.visualTreeAsset = settingsMenuUXML;
        _root = _uiDocument.rootVisualElement;

        StartCoroutine(InitializeSettingsCoroutine());
    }

    private IEnumerator InitializeSettingsCoroutine()
    {
        yield return new WaitForEndOfFrame();

        InitializeSettingsUI();
        LocalizationManager.Instance?.RefreshForNewScene();
        LocalizationManager.Instance?.UpdateAllUIElements();

        Debug.Log("Settings screen initialized");
    }

    public void HideSettingsScreen()
    {
        if (!_isInSettingsMode) return;

        if (mainMenuUXML != null)
        {
            // Возвращаем основной UXML
            _uiDocument.visualTreeAsset = mainMenuUXML;
            _root = _uiDocument.rootVisualElement;

            StartCoroutine(ReinitializeMainMenuCoroutine());
        }
    }

    private IEnumerator ReinitializeMainMenuCoroutine()
    {
        yield return new WaitForEndOfFrame();

        // Переинициализируем главное меню
        InitializeMainMenuUI();

        Debug.Log("Returned to main menu");
    }

    private void OnSaveSettings()
    {
        Debug.Log("Settings saved");
        // Логика сохранения настроек будет в UISettingsManager
        HideSettingsScreen();
    }

    private void OnCancelSettings()
    {
        Debug.Log("Settings cancelled");
        HideSettingsScreen();
    }

    // Остальные методы остаются без изменений
    public void UpdatePlayerList()
    {
        _screenManager?.UpdatePlayerList();
    }

    public void OnLobbyCreated()
    {
        _screenManager?.ShowScreen(UIScreenManager.LobbySettingsScreenName);
        _lobbySettings?.SyncAllSettings();
        UIManager.Instance.OnPlayersUpdated();
    }

    public void OnLobbyListUpdated() => _screenManager?.RefreshLobbyList();

    public void OnJoinedAsClient()
    {
        _screenManager?.ShowScreen("lobby_settings_screen");
        SetupClientModeUI();
    }

    public void SetupHostModeUI()
    {
        if (!_isInSettingsMode && _root != null)
        {
            var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
            if (lobbySettingsScreen == null) return;

            var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
            var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
            var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
            var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

            if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.Flex;
            if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.Flex;
            if (lobbyNameField != null) lobbyNameField.SetEnabled(true);
            if (playerCountSlider != null) playerCountSlider.SetEnabled(true);
        }
    }

    public void ReturnToLobbyList()
    {
        if (!_isInSettingsMode)
        {
            _screenManager?.ShowScreen(UIScreenManager.LobbyListScreenName);
            StartCoroutine(DelayedLobbyRefresh2());
        }
    }

    private IEnumerator DelayedLobbyRefresh()
    {
        yield return new WaitForEndOfFrame();
        UIManager.Instance.OnLobbyListUpdated();
        LobbyDiscovery.Instance?.ForceDiscovery();
    }

    private IEnumerator DelayedLobbyRefresh2()
    {
        yield return new WaitForSeconds(0.5f);
        UIManager.Instance?.OnLobbyListUpdated();
        LobbyDiscovery.Instance?.ForceDiscovery();
    }

    public void HandleLobbyClosed(string lobbyId)
    {
        if (!_isInSettingsMode && GetCurrentScreen() == UIScreenManager.LobbySettingsScreenName)
        {
            StartCoroutine(HandleLobbyClosedCoroutine(lobbyId));
        }
    }

    private IEnumerator HandleLobbyClosedCoroutine(string lobbyId)
    {
        yield return new WaitForSeconds(0.5f);
        ReturnToLobbyList();
    }

    public string GetCurrentScreen()
    {
        return _isInSettingsMode ? "settings_screen" : (_screenManager?.GetCurrentScreen() ?? "unknown");
    }

    public void ShowLobbyListAfterDisband()
    {
        if (!_isInSettingsMode)
        {
            StartCoroutine(ShowLobbyListAfterDisbandCoroutine());
        }
    }

    private IEnumerator ShowLobbyListAfterDisbandCoroutine()
    {
        yield return new WaitForSeconds(1f);
        _screenManager?.ShowScreen(UIScreenManager.LobbyListScreenName);
        UIManager.Instance?.OnLobbyListUpdated();
        LobbyDiscovery.Instance?.ForceDiscovery();
    }

    private void SetupClientModeUI()
    {
        if (!_isInSettingsMode && _root != null)
        {
            var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
            if (lobbySettingsScreen == null) return;

            var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
            var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
            var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
            var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

            if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.None;
            if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.None;
            if (lobbyNameField != null) lobbyNameField.SetEnabled(false);
            if (playerCountSlider != null) playerCountSlider.SetEnabled(false);
        }
    }

    private void OnDestroy()
    {
        _inputManager?.Cleanup();
        _settingsManager?.Cleanup();
    }
}