using UnityEngine;
using UnityEngine.UIElements;
using Unity.NetCode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private UIDocument _uiDocument;
    private VisualElement _root;

    // UI Elements
    private Slider _playerCountSlider;
    private Label _playerCountValue;
    private Slider _waveCountSlider;
    private RadioButton _waveCountRadio;
    private RadioButton _infinityRadio;
    private RadioButton _radioOpen;
    private RadioButton _radioClosed;
    private VisualElement _charVacuum;
    private VisualElement _charToaster;
    private VisualElement _charGPT;

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
            Debug.Log("UIManager initialized successfully");
        }
        else
        {
            Destroy(gameObject);
        }
    }

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
        _waveCountRadio = _root.Q<RadioButton>("waveCountRadio");
        _infinityRadio = _root.Q<RadioButton>("infinityRadio");
        _radioOpen = _root.Q<RadioButton>("radioOpen");
        _radioClosed = _root.Q<RadioButton>("radioClosed");
        
        // Находим элементы выбора персонажа
        _charVacuum = _root.Q<VisualElement>("charVacuum");
        _charToaster = _root.Q<VisualElement>("charToaster");
        _charGPT = _root.Q<VisualElement>("charGPT");

        // Добавляем обработчики
        if (btnSingle != null) btnSingle.clicked += OnSinglePlayer;
        if (btnCreateLobby != null) btnCreateLobby.clicked += OnCreateLobby;
        if (btnStatistics != null) btnStatistics.clicked += OnStatistics;
        if (btnQuit != null) btnQuit.clicked += OnQuit;

        // Настраиваем слайдер количества игроков
        if (_playerCountSlider != null && _playerCountValue != null)
        {
            _playerCountSlider.RegisterValueChangedCallback(OnPlayerCountChanged);
            UpdatePlayerCountValue(_playerCountSlider.value);
        }

        // Настраиваем слайдер количества волн с шагом 50
        if (_waveCountSlider != null)
        {
            _waveCountSlider.highValue = 50;
            _waveCountSlider.lowValue = 10;
            _waveCountSlider.value = 10;
            
            // Устанавливаем шаг 10
            _waveCountSlider.RegisterValueChangedCallback(evt =>
            {
                // Округляем значение до ближайшего шага 10
                float roundedValue = Mathf.Round(evt.newValue / 10f) * 10f;
                if (Mathf.Abs(roundedValue - evt.newValue) > 0.1f)
                {
                    _waveCountSlider.value = roundedValue;
                }
            });
        }

        // Настраиваем радио-кнопки для волн
        if (_waveCountRadio != null && _infinityRadio != null)
        {
            _waveCountRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _infinityRadio.value = false;
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(true);
                }
            });

            _infinityRadio.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _waveCountRadio.value = false;
                    if (_waveCountSlider != null)
                        _waveCountSlider.SetEnabled(false);
                }
            });

            // По умолчанию выбираем волны
            _waveCountRadio.value = true;
            _infinityRadio.value = false;
        }

        // Настраиваем радио-кнопки типа лобби
        if (_radioOpen != null && _radioClosed != null)
        {
            _radioOpen.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioClosed.value = false;
                }
            });

            _radioClosed.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                {
                    _radioOpen.value = false;
                }
            });

            // По умолчанию выбираем открытое лобби
            _radioOpen.value = true;
            _radioClosed.value = false;
        }

        // Настраиваем выбор персонажа
        SetupCharacterSelection();

        // Показываем главный экран
        ShowScreen("screen_main");
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

    // Методы для переключения экранов
    public void ShowScreen(string screenName)
    {
        var screens = _root.Query<VisualElement>(className: "screen").ToList();
        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        Debug.Log($"Showing screen: {screenName}");
    }

    // Обработчики кнопок
    private void OnSinglePlayer()
    {
        Debug.Log("Starting single player...");
        ShowScreen("screen_main");

        // Здесь запускаем одиночную игру
        // ClientServerBootstrap.CreateServerWorld(World.DefaultGameObjectInjectionWorld);
    }

    private void OnCreateLobby()
    {
        Debug.Log("Creating lobby...");
        ShowScreen("screen_lobby"); // Открываем экран лобби вместо screen_create
    }

    private void OnBrowseLobbies()
    {
        Debug.Log("Browsing lobbies...");
        ShowScreen("screen_lobby"); // Открываем экран лобби
    }

    private void OnStatistics()
    {
        Debug.Log("Opening statistics...");
        // Здесь можно добавить логику для статистики
        // Пока остаемся на главном экране или переходим на экран статистики
    }

    private void OnQuit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Методы для сетевых событий
    public void OnConnectedToServer()
    {
        Debug.Log("Connected to server");
        ShowScreen("screen_lobby");
    }

    public void OnDisconnectedFromServer()
    {
        Debug.Log("Disconnected from server");
        ShowScreen("screen_main");
    }
}