using UnityEngine;
using UnityEngine.UIElements;
using Unity.NetCode;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private UIDocument _uiDocument;
    private VisualElement _root;

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

        // Добавляем обработчики
        if (btnSingle != null) btnSingle.clicked += OnSinglePlayer;
        if (btnCreateLobby != null) btnCreateLobby.clicked += OnCreateLobby;
        if (btnStatistics != null) btnStatistics.clicked += OnStatistics;
        if (btnQuit != null) btnQuit.clicked += OnQuit;

        // Показываем главный экран
        ShowScreen("screen_main");
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