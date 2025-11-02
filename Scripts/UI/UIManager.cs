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

    // UI State
    private string _currentScreen = "screen_main";
    private Button _currentlyFocusedButton;
    private bool _isSettingFocus = false; // Флаг для предотвращения рекурсии

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

            // Принудительно показываем UI
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

        // Получаем карту действий для UI
        _uiActionMap = inputActions.FindActionMap("UI");
        
        if (_uiActionMap == null)
        {
            Debug.LogError("UI Action Map not found in InputActionAsset!");
            return;
        }

        // Находим конкретные действия
        _navigateAction = _uiActionMap.FindAction("Navigate");
        _submitAction = _uiActionMap.FindAction("Submit");
        _cancelAction = _uiActionMap.FindAction("Cancel");

        // Проверяем что все действия найдены
        if (_navigateAction == null) Debug.LogError("Navigate action not found!");
        if (_submitAction == null) Debug.LogError("Submit action not found!");
        if (_cancelAction == null) Debug.LogError("Cancel action not found!");

        // Подписываемся на события
        SubscribeToInputEvents();
    }

    private void SubscribeToInputEvents()
    {
        // Обработка Cancel (Escape)
        if (_cancelAction != null)
            _cancelAction.performed += OnCancelPerformed;

        // Обработка навигации (клавиши/геймпад)
        if (_navigateAction != null)
            _navigateAction.performed += OnNavigatePerformed;

        // Обработка подтверждения (Enter/кнопка геймпада)
        if (_submitAction != null)
            _submitAction.performed += OnSubmitPerformed;
    }

    private void EnableInputSystem()
    {
        if (inputActions == null) return;

        // Отключаем все карты действий и включаем только UI
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
        if (_isSettingFocus) return; // Игнорируем навигацию во время установки фокуса

        // Навигация по UI с помощью клавиатуры/геймпада
        Vector2 direction = context.ReadValue<Vector2>();
        
        if (Mathf.Abs(direction.y) > 0.1f)
        {
            // Навигация вверх/вниз
            NavigateUI(Mathf.RoundToInt(-direction.y));
        }
        else if (Mathf.Abs(direction.x) > 0.1f)
        {
            // Навигация влево/вправо (для радио-кнопок и других горизонтальных элементов)
            NavigateHorizontal(Mathf.RoundToInt(direction.x));
        }
    }

    private void OnSubmitPerformed(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Активируем текущую выбранную кнопку
        if (_currentlyFocusedButton != null && _currentlyFocusedButton.enabledSelf)
        {
            // Определяем какая кнопка сейчас в фокусе и вызываем соответствующий метод
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
                    // Для неизвестных кнопок просто симулируем клик через событие
                    SimulateButtonClick(_currentlyFocusedButton);
                    break;
            }
        }
    }

    private void SimulateButtonClick(Button button)
    {
        // Создаем и отправляем событие клика
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
                // Возвращаемся в главное меню из лобби
                ReturnToMainMenu();
                Debug.Log("Returned to main menu from lobby");
                break;
            
            case "screen_main":
                // В главном меню - выходим из игры
                StartCoroutine(QuitWithConfirmation());
                break;
            
            default:
                // Для других экранов - возвращаемся в главное меню
                ReturnToMainMenu();
                break;
        }
    }

    #endregion

    #region UI Navigation

    private void NavigateUI(int direction)
    {
        // Получаем все интерактивные элементы на текущем экране
        var interactiveElements = _root.Query<Button>().Where(btn => 
            btn.resolvedStyle.display == DisplayStyle.Flex && 
            btn.enabledSelf &&
            btn.visible &&
            btn.focusable).ToList();

        if (interactiveElements.Count == 0) return;

        // Находим текущий индекс
        int currentIndex = _currentlyFocusedButton != null ? 
            interactiveElements.IndexOf(_currentlyFocusedButton) : -1;

        // Вычисляем новый индекс
        int newIndex = currentIndex + direction;
        if (newIndex < 0) newIndex = interactiveElements.Count - 1;
        if (newIndex >= interactiveElements.Count) newIndex = 0;

        // Обновляем фокус
        SetFocusToButton(interactiveElements[newIndex]);
    }

    private void NavigateHorizontal(int direction)
    {
        // Можно добавить логику для горизонтальной навигации
        // между радио-кнопками или другими элементами
    }

    private void SetFocusToButton(Button button)
    {
        if (_isSettingFocus || button == null) return;

        _isSettingFocus = true;

        try
        {
            // Убираем фокус с предыдущей кнопки
            if (_currentlyFocusedButton != null && _currentlyFocusedButton != button)
            {
                _currentlyFocusedButton.Blur();
                _currentlyFocusedButton.RemoveFromClassList("focused");
            }

            // Устанавливаем фокус на новую кнопку
            _currentlyFocusedButton = button;
            
            // Убедимся что кнопка может получать фокус
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

        // Добавляем обработчики кнопок
        SetupButtonCallbacks();

        // Настраиваем слайдеры
        SetupSliders();

        // Настраиваем радио-кнопки
        SetupWaveRadioButtons();
        SetupLobbyRadioButtons();

        // Настраиваем выбор персонажа
        SetupCharacterSelection();

        // Показываем главный экран
        ShowScreen("screen_main");

        // Устанавливаем начальный фокус
        StartCoroutine(SetInitialFocus());
    }

    private IEnumerator SetInitialFocus()
    {
        // Ждем один кадр чтобы UI полностью инициализировался
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

        if (btnSingle != null) 
        {
            btnSingle.clicked += OnSinglePlayer;
        }
        if (btnCreateLobby != null) 
        {
            btnCreateLobby.clicked += OnCreateLobby;
        }
        if (btnStatistics != null) 
        {
            btnStatistics.clicked += OnStatistics;
        }
        if (btnQuit != null) 
        {
            btnQuit.clicked += OnQuit;
        }
    }

    private void SetupSliders()
    {
        // Настраиваем слайдер количества игроков
        if (_playerCountSlider != null && _playerCountValue != null)
        {
            _playerCountSlider.RegisterValueChangedCallback(OnPlayerCountChanged);
            UpdatePlayerCountValue(_playerCountSlider.value);
        }

        // Настраиваем слайдер количества волн с шагом 10
        if (_waveCountSlider != null && _waveCountValue != null)
        {
            _waveCountSlider.lowValue = 10;
            _waveCountSlider.highValue = 50;
            _waveCountSlider.value = 10;

            _waveCountSlider.RegisterValueChangedCallback(evt =>
            {
                // Округляем значение до ближайшего шага 10
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
            // Сбрасываем все значения
            _waveCountRadio.SetValueWithoutNotify(false);
            _infinityRadio.SetValueWithoutNotify(false);

            // Устанавливаем независимые обработчики для группы волн
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
                    // Если ни одна не выбрана, принудительно выбираем волны
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
                    // Если ни одна не выбрана, принудительно выбираем бесконечность
                    _infinityRadio.SetValueWithoutNotify(true);
                }
            });

            // По умолчанию выбираем волны
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
            // Сбрасываем все значения
            _radioOpen.SetValueWithoutNotify(false);
            _radioClosed.SetValueWithoutNotify(false);

            // Устанавливаем независимые обработчики для группы лобби
            _radioOpen.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioClosed.SetValueWithoutNotify(false);
                }
                else if (!_radioClosed.value)
                {
                    // Если ни одна не выбрана, принудительно выбираем открытое
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
                    // Если ни одна не выбрана, принудительно выбираем закрытое
                    _radioClosed.SetValueWithoutNotify(true);
                }
            });

            // По умолчанию выбираем открытое лобби
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
        // Убираем selected у всех персонажей
        if (_charVacuum != null) _charVacuum.RemoveFromClassList("selected");
        if (_charToaster != null) _charToaster.RemoveFromClassList("selected");
        if (_charGPT != null) _charGPT.RemoveFromClassList("selected");

        // Добавляем selected выбранному персонажу
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
        
        // Сбрасываем фокус при смене экрана
        _currentlyFocusedButton = null;
        
        // Устанавливаем фокус на первую кнопку нового экрана
        StartCoroutine(SetInitialFocus());
        
        Debug.Log($"Showing screen: {screenName}");
    }

    private void ReturnToMainMenu()
    {
        ShowScreen("screen_main");
    }

    private IEnumerator QuitWithConfirmation()
    {
        // Можно добавить диалог подтверждения
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
        // Переключаемся на Player Action Map для игрового процесса
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

        // Отключаем UI Input и включаем Player Input
        _uiActionMap?.Disable();
        var playerActionMap = inputActions.FindActionMap("Player");
        playerActionMap?.Enable();
        Debug.Log("Switched to Player input");
    }

    public void SwitchToUIInput()
    {
        if (inputActions == null) return;

        // Отключаем Player Input и включаем UI Input
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