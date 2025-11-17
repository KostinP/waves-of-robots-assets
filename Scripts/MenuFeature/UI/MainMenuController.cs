using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using UnityEngine.InputSystem;

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

    [Header("Fallback Settings")]
    [SerializeField] private InputActionAsset fallbackInputActions;

    private bool _isInSettingsMode = false;
    private bool _isInitialized = false;

    public UILobbySetupManager LobbySetupManager => _lobbySetup;
    public UICharacterSelectionManager CharacterSelectionManager => _charSelect;

    private void Start()
    {
        Debug.Log("MainMenuController: Start called");
        
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
        else
        {
            Debug.LogError("MainMenu UXML not assigned!");
            return;
        }

        _root = _uiDocument.rootVisualElement;
        
        if (_root == null)
        {
            Debug.LogError("Root VisualElement is null! Starting delayed initialization.");
            StartCoroutine(InitializeCoroutine());
            return;
        }

        // Пытаемся инициализировать сразу
        StartCoroutine(InitializeCoroutine());
    }

    private IEnumerator InitializeCoroutine()
    {
        Debug.Log("MainMenuController: Starting initialization coroutine");

        // Ждем инициализации UIManager
        yield return StartCoroutine(WaitForUIManager());

        // Даем дополнительное время для UI
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame();

        // Переполучаем ссылку на root после ожидания
        _root = _uiDocument.rootVisualElement;
        
        if (_root == null)
        {
            Debug.LogError("Root VisualElement is still null after waiting!");
            yield break;
        }

        InitializeMainMenuUI();
        _isInitialized = true;
        Debug.Log("MainMenuController: Initialization completed successfully");
    }

    private IEnumerator WaitForUIManager()
    {
        Debug.Log("Waiting for UIManager...");
        
        int maxWait = 50;
        int currentWait = 0;
        
        while (UIManager.Instance == null && currentWait < maxWait)
        {
            currentWait++;
            yield return new WaitForEndOfFrame();
        }
        
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance is null after waiting. Using fallback initialization.");
        }
        else
        {
            Debug.Log("UIManager.Instance ready after " + currentWait + " frames");
        }
    }

    private void InitializeMainMenuUI()
    {
        try
        {
            Debug.Log("Initializing MainMenuUI...");

            // Получаем InputActions (основные или резервные)
            InputActionAsset inputActions = GetInputActions();
            if (inputActions == null)
            {
                Debug.LogError("No InputActions available! Aborting initialization.");
                return;
            }

            Debug.Log("Creating UI managers...");

            // Инициализируем менеджеры с безопасным созданием
            _inputManager = SafeCreateManager(() => new UIInputManager(inputActions, _root, this), "UIInputManager");
            _screenManager = SafeCreateManager(() => new UIScreenManager(_root, this), "UIScreenManager");
            _lobbySetup = SafeCreateManager(() => new UILobbySetupManager(_root, this), "UILobbySetupManager");
            _charSelect = SafeCreateManager(() => new UICharacterSelectionManager(_root), "UICharacterSelectionManager");
            _lobbySettings = SafeCreateManager(() => new LobbySettingsManager(_root), "LobbySettingsManager");

            // Проверяем, что все менеджеры созданы
            if (_inputManager == null || _screenManager == null || _lobbySetup == null || _charSelect == null || _lobbySettings == null)
            {
                Debug.LogError("One or more UI managers failed to initialize!");
                return;
            }

            Debug.Log("Enabling input manager...");
            _inputManager.Enable();

            Debug.Log("Showing main menu screen...");
            _screenManager.ShowScreen(UIScreenManager.MenuScreenName);

            // Локализация
            if (LocalizationManager.Instance != null)
            {
                LocalizationManager.Instance.RefreshForNewScene();
                LocalizationManager.Instance.UpdateAllUIElements();
            }
            else
            {
                Debug.LogWarning("LocalizationManager.Instance is null");
            }

            _isInSettingsMode = false;
            Debug.Log("MainMenuUI initialization completed successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Critical error in InitializeMainMenuUI: {e.Message}\n{e.StackTrace}");
        }
    }

    private T SafeCreateManager<T>(System.Func<T> creator, string managerName) where T : class
    {
        try
        {
            Debug.Log($"Creating {managerName}...");
            var manager = creator();
            if (manager == null)
            {
                Debug.LogError($"Failed to create {managerName} - constructor returned null");
                return null;
            }
            else
            {
                Debug.Log($"Successfully created {managerName}");
                return manager;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Exception creating {managerName}: {e.Message}\n{e.StackTrace}");
            return null;
        }
    }

    private InputActionAsset GetInputActions()
    {
        // Пытаемся получить из UIManager
        if (UIManager.Instance != null && UIManager.Instance.InputActions != null)
        {
            Debug.Log("Using InputActions from UIManager");
            return UIManager.Instance.InputActions;
        }

        // Используем резервные InputActions
        if (fallbackInputActions != null)
        {
            Debug.LogWarning("Using fallback InputActions");
            return fallbackInputActions;
        }

        // Создаем пустые InputActions как последнее средство
        Debug.LogError("No InputActions available! Creating empty fallback.");
        return CreateEmptyInputActions();
    }

    private InputActionAsset CreateEmptyInputActions()
    {
        try
        {
            // Создаем базовые InputActions чтобы избежать ошибок
            var inputActions = ScriptableObject.CreateInstance<InputActionAsset>();
            inputActions.name = "EmptyInputActions_Fallback";
            
            // Создаем простую карту действий чтобы не было ошибок
            var actionMap = new InputActionMap("UI");
            var navigateAction = actionMap.AddAction("Navigate", type: InputActionType.Value);
            var submitAction = actionMap.AddAction("Submit", type: InputActionType.Button);
            var cancelAction = actionMap.AddAction("Cancel", type: InputActionType.Button);
            
            inputActions.AddActionMap(actionMap);
            
            Debug.LogWarning("Created empty fallback InputActions");
            return inputActions;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to create empty InputActions: {e.Message}");
            return null;
        }
    }

    private void InitializeSettingsUI()
    {
        try
        {
            if (_root == null)
            {
                Debug.LogError("Cannot initialize SettingsUI: Root VisualElement is null!");
                return;
            }

            Debug.Log("Initializing Settings UI...");

            // Инициализируем менеджер настроек
            _settingsManager = new UISettingsManager(_root, _uiDocument, this);

            // Настраиваем обработчики для кнопок настроек
            var btnSave = _root.Q<Button>("btnSave");
            var btnCancel = _root.Q<Button>("cancelBtn");

            if (btnSave != null)
            {
                btnSave.clicked += OnSaveSettings;
                Debug.Log("Save button found and registered");
            }
            else
            {
                Debug.LogWarning("Save button not found in settings UI");
            }

            if (btnCancel != null)
            {
                btnCancel.clicked += OnCancelSettings;
                Debug.Log("Cancel button found and registered");
            }
            else
            {
                Debug.LogWarning("Cancel button not found in settings UI");
            }

            _isInSettingsMode = true;
            Debug.Log("Settings UI initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in InitializeSettingsUI: {e.Message}\n{e.StackTrace}");
        }
    }

    public void ShowScreen(string screenName)
    {
        if (!_isInitialized)
        {
            Debug.LogWarning("MainMenuController not initialized yet. Cannot show screen: " + screenName);
            return;
        }

        Debug.Log($"Showing screen: {screenName}");

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

        Debug.Log("Switching to settings screen...");

        // Переключаем на UXML настроек
        _uiDocument.visualTreeAsset = settingsMenuUXML;
        _root = _uiDocument.rootVisualElement;

        StartCoroutine(InitializeSettingsCoroutine());
    }

    private IEnumerator InitializeSettingsCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Дополнительное ожидание

        InitializeSettingsUI();
        
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RefreshForNewScene();
            LocalizationManager.Instance.UpdateAllUIElements();
        }

        Debug.Log("Settings screen initialized");
    }

    public void HideSettingsScreen()
    {
        if (!_isInSettingsMode) return;

        Debug.Log("Hiding settings screen...");

        if (mainMenuUXML != null)
        {
            // Возвращаем основной UXML
            _uiDocument.visualTreeAsset = mainMenuUXML;
            _root = _uiDocument.rootVisualElement;

            StartCoroutine(ReinitializeMainMenuCoroutine());
        }
        else
        {
            Debug.LogError("MainMenu UXML not assigned! Cannot return to main menu.");
        }
    }

    private IEnumerator ReinitializeMainMenuCoroutine()
    {
        yield return new WaitForEndOfFrame();
        yield return new WaitForEndOfFrame(); // Дополнительное ожидание

        // Переинициализируем главное меню
        InitializeMainMenuUI();

        Debug.Log("Returned to main menu");
    }

    private void OnSaveSettings()
    {
        Debug.Log("Settings saved");
        
        // Сохраняем настройки через менеджер
        _settingsManager?.SaveSettings();
        
        HideSettingsScreen();
    }

    private void OnCancelSettings()
    {
        Debug.Log("Settings cancelled");
        
        // Отменяем изменения через менеджер
        _settingsManager?.CancelSettings();
        
        HideSettingsScreen();
    }

    public void UpdatePlayerList()
    {
        _screenManager?.UpdatePlayerList();
    }

    public void OnLobbyCreated()
    {
        Debug.Log("MainMenuController: Lobby created");
        _screenManager?.ShowScreen(UIScreenManager.LobbySettingsScreenName);
        _lobbySettings?.SyncAllSettings();
        UIManager.Instance?.OnPlayersUpdated();
    }

    public void OnLobbyListUpdated()
    {
        Debug.Log("MainMenuController: Lobby list updated");
        _screenManager?.RefreshLobbyList();
    }

    public void OnJoinedAsClient()
    {
        Debug.Log("MainMenuController: Joined as client");

        _screenManager?.ShowScreen("lobby_settings_screen");
        SetupClientModeUI();

        Debug.Log("OnJoinedAsClient: Setting up client UI and starting player list monitoring");

        // Усиленное обновление списка игроков
        StartCoroutine(EnhancedPlayerListUpdate());
    }

    private IEnumerator EnhancedPlayerListUpdate()
    {
        yield return new WaitForEndOfFrame();

        // Многократное обновление с задержками
        for (int i = 0; i < 3; i++)
        {
            UpdatePlayerList();
            yield return new WaitForSeconds(1f);
        }

        Debug.Log("Enhanced player list update completed");
    }

    public void SetupHostModeUI()
    {
        if (!_isInSettingsMode && _root != null)
        {
            var lobbySettingsScreen = _root.Q<VisualElement>("lobby_settings_screen");
            if (lobbySettingsScreen == null)
            {
                Debug.LogWarning("Lobby settings screen not found for host mode setup");
                return;
            }

            var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
            var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
            var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
            var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

            if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.Flex;
            if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.Flex;
            if (lobbyNameField != null) lobbyNameField.SetEnabled(true);
            if (playerCountSlider != null) playerCountSlider.SetEnabled(true);

            Debug.Log("Host mode UI setup completed");
        }
    }

    public void ReturnToLobbyList()
    {
        if (!_isInSettingsMode && _isInitialized)
        {
            Debug.Log("Returning to lobby list");
            _screenManager?.ShowScreen(UIScreenManager.LobbyListScreenName);
            StartCoroutine(DelayedLobbyRefresh2());
        }
    }

    private IEnumerator DelayedLobbyRefresh()
    {
        yield return new WaitForEndOfFrame();
        UIManager.Instance?.OnLobbyListUpdated();
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
            Debug.Log($"Handling lobby closed: {lobbyId}");
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
        if (!_isInSettingsMode && _isInitialized)
        {
            Debug.Log("Showing lobby list after disband");
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
            if (lobbySettingsScreen == null)
            {
                Debug.LogWarning("Lobby settings screen not found for client mode setup");
                return;
            }

            var btnStartGame = lobbySettingsScreen.Q<Button>("btnStartGame");
            var btnDisbandLobby = lobbySettingsScreen.Q<Button>("btnDisbandLobby");
            var lobbyNameField = lobbySettingsScreen.Q<TextField>("lobbyNameField");
            var playerCountSlider = lobbySettingsScreen.Q<Slider>("playerCountSlider");

            if (btnStartGame != null) btnStartGame.style.display = DisplayStyle.None;
            if (btnDisbandLobby != null) btnDisbandLobby.style.display = DisplayStyle.None;
            if (lobbyNameField != null) lobbyNameField.SetEnabled(false);
            if (playerCountSlider != null) playerCountSlider.SetEnabled(false);

            Debug.Log("Client mode UI setup completed");
        }
    }

    private void OnDestroy()
    {
        Debug.Log("MainMenuController: OnDestroy called");
        
        _inputManager?.Cleanup();
        _settingsManager?.Cleanup();
        
        // Отписываемся от событий
        if (_root != null)
        {
            var btnSave = _root.Q<Button>("btnSave");
            var btnCancel = _root.Q<Button>("cancelBtn");
            
            if (btnSave != null) btnSave.clicked -= OnSaveSettings;
            if (btnCancel != null) btnCancel.clicked -= OnCancelSettings;
        }
    }

    // Метод для принудительной переинициализации (например, после загрузки сцены)
    public void ForceReinitialize()
    {
        if (_isInitialized)
        {
            Debug.Log("Forcing reinitialization...");
            _isInitialized = false;
            StartCoroutine(InitializeCoroutine());
        }
    }

    // Свойство для проверки состояния инициализации
    public bool IsInitialized => _isInitialized;
}