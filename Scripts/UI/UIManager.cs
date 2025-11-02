using UnityEngine;
using UnityEngine.UIElements;
using Unity.NetCode;
using UnityEngine.InputSystem;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Input System")]
    [SerializeField] private InputActionAsset inputActions;

    // UI Elements
    private UIDocument _uiDocument;
    private VisualElement _root;
    
    // Input System
    private InputActionMap _uiActionMap;
    private InputAction _navigateAction;
    private InputAction _submitAction;
    private InputAction _cancelAction;
    private InputAction _openSettingsAction;
    private InputAction _openStatisticsAction;

    // UI State
    private string _currentScreen = "screen_main";
    private Button _currentlyFocusedButton;
    private bool _isSettingFocus = false;

    // UI Elements
    private Slider _playerCountSlider;
    private Label _playerCountValue;
    private Slider _waveCountSlider;
    private Label _waveCountValue;
    private RadioButton _waveCountRadio;
    private RadioButton _infinityRadio;
    private RadioButton _radioOpen;
    private RadioButton _radioClosed;
    private VisualElement _charVacuum;
    private VisualElement _charToaster;
    private VisualElement _charGPT;

    #region Lifecycle Methods

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _uiDocument = GetComponent<UIDocument>();
            if (_uiDocument == null)
            {
                Debug.LogError("UIDocument not found!");
                return;
            }

            _root = _uiDocument.rootVisualElement;
            if (_root == null)
            {
                Debug.LogError("Root visual element is null!");
                return;
            }

            _root.style.display = DisplayStyle.Flex;

            InitializeUI();
            InitializeInputSystem();
            Debug.Log("UIManager initialized successfully");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        EnableInputSystem();
    }

    private void OnDisable()
    {
        DisableInputSystem();
    }

    private void OnDestroy()
    {
        CleanupInputSystem();
    }

    #endregion

    #region Input System Management

    private void InitializeInputSystem()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("InputActionAsset is not assigned! Assign your InputSystem_Actions asset in the inspector. Input will be disabled.");
            return;
        }

        _uiActionMap = inputActions.FindActionMap("UI");
        
        if (_uiActionMap == null)
        {
            Debug.LogError("UI Action Map not found in InputActionAsset!");
            return;
        }

        // Находим все действия
        _navigateAction = _uiActionMap.FindAction("Navigate");
        _submitAction = _uiActionMap.FindAction("Submit");
        _cancelAction = _uiActionMap.FindAction("Cancel");
        _openSettingsAction = _uiActionMap.FindAction("OpenSettings");
        _openStatisticsAction = _uiActionMap.FindAction("OpenStatistics");

        // Проверяем что основные действия найдены
        if (_navigateAction == null) Debug.LogError("Navigate action not found!");
        if (_submitAction == null) Debug.LogError("Submit action not found!");
        if (_cancelAction == null) Debug.LogError("Cancel action not found!");

        // Новые действия опциональны, но логируем если не найдены
        if (_openSettingsAction == null) Debug.LogWarning("OpenSettings action not found!");
        if (_openStatisticsAction == null) Debug.LogWarning("OpenStatistics action not found!");

        SubscribeToInputEvents();
    }

    private void SubscribeToInputEvents()
    {
        // Основные действия UI
        if (_cancelAction != null)
            _cancelAction.performed += OnCancelPerformed;

        if (_navigateAction != null)
            _navigateAction.performed += OnNavigatePerformed;

        if (_submitAction != null)
            _submitAction.performed += OnSubmitPerformed;

        // Новые быстрые действия
        if (_openSettingsAction != null)
            _openSettingsAction.performed += OnOpenSettingsPerformed;

        if (_openStatisticsAction != null)
            _openStatisticsAction.performed += OnOpenStatisticsPerformed;
    }

    private void EnableInputSystem()
    {
        if (inputActions == null) return;

        inputActions.Disable();
        _uiActionMap?.Enable();
    }

    private void DisableInputSystem()
    {
        _uiActionMap?.Disable();
    }

    private void CleanupInputSystem()
    {
        // Отписываемся от всех событий
        if (_cancelAction != null) _cancelAction.performed -= OnCancelPerformed;
        if (_navigateAction != null) _navigateAction.performed -= OnNavigatePerformed;
        if (_submitAction != null) _submitAction.performed -= OnSubmitPerformed;
        if (_openSettingsAction != null) _openSettingsAction.performed -= OnOpenSettingsPerformed;
        if (_openStatisticsAction != null) _openStatisticsAction.performed -= OnOpenStatisticsPerformed;
    }

    #endregion

    #region Input Event Handlers

    private void OnCancelPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        HandleEscapeKey();
    }

    private void OnNavigatePerformed(InputAction.CallbackContext context)
    {
        if (_isSettingFocus) return;

        Vector2 direction = context.ReadValue<Vector2>();
        
        if (Mathf.Abs(direction.y) > 0.1f)
        {
            NavigateUI(Mathf.RoundToInt(-direction.y));
        }
        else if (Mathf.Abs(direction.x) > 0.1f)
        {
            NavigateHorizontal(Mathf.RoundToInt(direction.x));
        }
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        if (_currentlyFocusedButton != null && _currentlyFocusedButton.enabledSelf)
        {
            string buttonName = _currentlyFocusedButton.name;
            
            switch (buttonName)
            {
                case "btnSingle":
                    OnSinglePlayer();
                    break;
                case "btnCreateLobby":
                    OnCreateLobby();
                    break;
                case "btnStatistics":
                    OnStatistics();
                    break;
                case "btnQuit":
                    OnQuit();
                    break;
                default:
                    SimulateButtonClick(_currentlyFocusedButton);
                    break;
            }
        }
    }

    // Новые обработчики для быстрых действий
    private void OnOpenSettingsPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        Debug.Log("OpenSettings action triggered");
        // Здесь можно добавить открытие экрана настроек
        // ShowScreen("screen_settings");
    }

    private void OnOpenStatisticsPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        
        Debug.Log("OpenStatistics action triggered");
        OnStatistics(); // Используем существующий метод
    }

    private void SimulateButtonClick(Button button)
    {
        using (var clickEvent = NavigationSubmitEvent.GetPooled())
        {
            clickEvent.target = button;
            button.SendEvent(clickEvent);
        }
    }

    private void HandleEscapeKey()
    {
        switch (_currentScreen)
        {
            case "screen_lobby":
                ReturnToMainMenu();
                Debug.Log("Returned to main menu from lobby");
                break;
            
            case "screen_main":
                StartCoroutine(QuitWithConfirmation());
                break;
            
            default:
                ReturnToMainMenu();
                break;
        }
    }

    #endregion

    #region UI Navigation

    private void NavigateUI(int direction)
    {
        var interactiveElements = _root.Query<Button>().Where(btn => 
            btn.resolvedStyle.display == DisplayStyle.Flex && 
            btn.enabledSelf &&
            btn.visible &&
            btn.focusable).ToList();

        if (interactiveElements.Count == 0) return;

        int currentIndex = _currentlyFocusedButton != null ? 
            interactiveElements.IndexOf(_currentlyFocusedButton) : -1;

        int newIndex = currentIndex + direction;
        if (newIndex < 0) newIndex = interactiveElements.Count - 1;
        if (newIndex >= interactiveElements.Count) newIndex = 0;

        SetFocusToButton(interactiveElements[newIndex]);
    }

    private void NavigateHorizontal(int direction)
    {
        // Логика для горизонтальной навигации
    }

    private void SetFocusToButton(Button button)
    {
        if (_isSettingFocus || button == null) return;

        _isSettingFocus = true;

        try
        {
            if (_currentlyFocusedButton != null && _currentlyFocusedButton != button)
            {
                _currentlyFocusedButton.Blur();
                _currentlyFocusedButton.RemoveFromClassList("focused");
            }

            _currentlyFocusedButton = button;
            
            if (!_currentlyFocusedButton.focusable)
                _currentlyFocusedButton.focusable = true;
                
            _currentlyFocusedButton.Focus();
            _currentlyFocusedButton.AddToClassList("focused");
        }
        finally
        {
            _isSettingFocus = false;
        }
    }

    #endregion

    #region UI Management

    private void InitializeUI()
    {
        // Находим все элементы
        var btnSingle = _root.Q<Button>("btnSingle");
        var btnCreateLobby = _root.Q<Button>("btnCreateLobby");
        var btnStatistics = _root.Q<Button>("btnStatistics");
        var btnQuit = _root.Q<Button>("btnQuit");

        // Находим элементы для слайдеров и радио-кнопок
        _playerCountSlider = _root.Q<Slider>("playerCountSlider");
        _playerCountValue = _root.Q<Label>("playerCountValue");
        _waveCountSlider = _root.Q<Slider>("waveCountSlider");
        _waveCountValue = _root.Q<Label>("waveCountValue");
        _waveCountRadio = _root.Q<RadioButton>("waveCountRadio");
        _infinityRadio = _root.Q<RadioButton>("infinityRadio");
        _radioOpen = _root.Q<RadioButton>("radioOpen");
        _radioClosed = _root.Q<RadioButton>("radioClosed");

        // Находим элементы выбора персонажа
        _charVacuum = _root.Q<VisualElement>("charVacuum");
        _charToaster = _root.Q<VisualElement>("charToaster");
        _charGPT = _root.Q<VisualElement>("charGPT");

        SetupButtonCallbacks();
        SetupSliders();
        SetupWaveRadioButtons();
        SetupLobbyRadioButtons();
        SetupCharacterSelection();

        ShowScreen("screen_main");
        StartCoroutine(SetInitialFocus());
    }

    private IEnumerator SetInitialFocus()
    {
        yield return null;
        
        var firstButton = _root.Q<Button>();
        if (firstButton != null && firstButton.enabledSelf && firstButton.visible)
        {
            SetFocusToButton(firstButton);
        }
    }

    private void SetupButtonCallbacks()
    {
        var btnSingle = _root.Q<Button>("btnSingle");
        var btnCreateLobby = _root.Q<Button>("btnCreateLobby");
        var btnStatistics = _root.Q<Button>("btnStatistics");
        var btnQuit = _root.Q<Button>("btnQuit");

        if (btnSingle != null) btnSingle.clicked += OnSinglePlayer;
        if (btnCreateLobby != null) btnCreateLobby.clicked += OnCreateLobby;
        if (btnStatistics != null) btnStatistics.clicked += OnStatistics;
        if (btnQuit != null) btnQuit.clicked += OnQuit;
    }

    private void SetupSliders()
    {
        if (_playerCountSlider != null && _playerCountValue != null)
        {
            _playerCountSlider.RegisterValueChangedCallback(OnPlayerCountChanged);
            UpdatePlayerCountValue(_playerCountSlider.value);
        }

        if (_waveCountSlider != null && _waveCountValue != null)
        {
            _waveCountSlider.lowValue = 10;
            _waveCountSlider.highValue = 50;
            _waveCountSlider.value = 10;

            _waveCountSlider.RegisterValueChangedCallback(evt =>
            {
                float roundedValue = Mathf.Round(evt.newValue / 10f) * 10f;
                if (Mathf.Abs(roundedValue - evt.newValue) > 0.1f)
                {
                    _waveCountSlider.value = roundedValue;
                }
                UpdateWaveCountValue(_waveCountSlider.value);
            });

            UpdateWaveCountValue(_waveCountSlider.value);
        }
    }

    private void SetupWaveRadioButtons()
    {
        if (_waveCountRadio != null && _infinityRadio != null)
        {
            _waveCountRadio.SetValueWithoutNotify(false);
            _infinityRadio.SetValueWithoutNotify(false);

            _waveCountRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _infinityRadio.SetValueWithoutNotify(false);
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(true);
                }
                else if (!_infinityRadio.value)
                {
                    _waveCountRadio.SetValueWithoutNotify(true);
                }
            });

            _infinityRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _waveCountRadio.SetValueWithoutNotify(false);
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(false);
                }
                else if (!_waveCountRadio.value)
                {
                    _infinityRadio.SetValueWithoutNotify(true);
                }
            });

            _waveCountRadio.value = true;
            _infinityRadio.value = false;
            if (_waveCountSlider != null)
                _waveCountSlider.SetEnabled(true);
        }
    }

    private void SetupLobbyRadioButtons()
    {
        if (_radioOpen != null && _radioClosed != null)
        {
            _radioOpen.SetValueWithoutNotify(false);
            _radioClosed.SetValueWithoutNotify(false);

            _radioOpen.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioClosed.SetValueWithoutNotify(false);
                }
                else if (!_radioClosed.value)
                {
                    _radioOpen.SetValueWithoutNotify(true);
                }
            });

            _radioClosed.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioOpen.SetValueWithoutNotify(false);
                }
                else if (!_radioOpen.value)
                {
                    _radioClosed.SetValueWithoutNotify(true);
                }
            });

            _radioOpen.value = true;
            _radioClosed.value = false;
        }
    }

    private void SetupCharacterSelection()
    {
        if (_charVacuum != null)
            _charVacuum.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charVacuum));
        if (_charToaster != null)
            _charToaster.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charToaster));
        if (_charGPT != null)
            _charGPT.RegisterCallback<ClickEvent>(evt => SelectCharacter(_charGPT));
    }

    private void SelectCharacter(VisualElement selectedCharacter)
    {
        if (_charVacuum != null) _charVacuum.RemoveFromClassList("selected");
        if (_charToaster != null) _charToaster.RemoveFromClassList("selected");
        if (_charGPT != null) _charGPT.RemoveFromClassList("selected");

        selectedCharacter.AddToClassList("selected");
        Debug.Log($"Selected character: {selectedCharacter.name}");
    }

    private void OnPlayerCountChanged(ChangeEvent<float> evt)
    {
        UpdatePlayerCountValue(evt.newValue);
    }

    private void UpdatePlayerCountValue(float value)
    {
        if (_playerCountValue != null)
        {
            _playerCountValue.text = Mathf.RoundToInt(value).ToString();
        }
    }

    private void UpdateWaveCountValue(float value)
    {
        if (_waveCountValue != null)
        {
            _waveCountValue.text = Mathf.RoundToInt(value).ToString();
        }
    }

    public void ShowScreen(string screenName)
    {
        var screens = _root.Query<VisualElement>(className: "screen").ToList();
        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }

        _currentScreen = screenName;
        _currentlyFocusedButton = null;
        StartCoroutine(SetInitialFocus());
        Debug.Log($"Showing screen: {screenName}");
    }

    private void ReturnToMainMenu()
    {
        ShowScreen("screen_main");
    }

    private IEnumerator QuitWithConfirmation()
    {
        Debug.Log("Quitting application...");
        yield return new WaitForSeconds(0.1f);
        
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    #endregion

    #region Button Handlers

    private void OnSinglePlayer()
    {
        Debug.Log("Starting single player...");
        SwitchToPlayerInput();
        ShowScreen("screen_main");
    }

    private void OnCreateLobby()
    {
        Debug.Log("Creating lobby...");
        ShowScreen("screen_lobby");
    }

    private void OnStatistics()
    {
        Debug.Log("Opening statistics...");
        // Здесь можно добавить логику для статистики
        // ShowScreen("screen_statistics");
    }

    private void OnQuit()
    {
        StartCoroutine(QuitWithConfirmation());
    }

    #endregion

    #region Input Switching

    public void SwitchToPlayerInput()
    {
        if (inputActions == null) return;

        _uiActionMap?.Disable();
        var playerActionMap = inputActions.FindActionMap("Player");
        playerActionMap?.Enable();
        Debug.Log("Switched to Player input");
    }

    public void SwitchToUIInput()
    {
        if (inputActions == null) return;

        var playerActionMap = inputActions.FindActionMap("Player");
        playerActionMap?.Disable();
        _uiActionMap?.Enable();
        Debug.Log("Switched to UI input");
    }

    #endregion

    #region Network Events

    public void OnConnectedToServer()
    {
        Debug.Log("Connected to server");
        ShowScreen("screen_lobby");
    }

    public void OnDisconnectedFromServer()
    {
        Debug.Log("Disconnected from server");
        ShowScreen("screen_main");
        SwitchToUIInput();
    }

    #endregion
}