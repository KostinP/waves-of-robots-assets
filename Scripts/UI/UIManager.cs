using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Dependencies")]
    [SerializeField] private InputActionAsset inputActions;

    private UIDocument _uiDocument;
    private VisualElement _root;

    // Подсистемы
    private UIInputManager _inputManager;
    private UIScreenManager _screenManager;
    private UIResponsiveManager _responsiveManager;
    private UILobbySetupManager _lobbySetupManager;
    private UICharacterSelectionManager _characterSelectionManager;

    #region Lifecycle Methods

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
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

        // Инициализация подсистем
        _inputManager = new UIInputManager(inputActions, _root);
        _screenManager = new UIScreenManager(_root);
        _responsiveManager = new UIResponsiveManager(_root);
        _lobbySetupManager = new UILobbySetupManager(_root);
        _characterSelectionManager = new UICharacterSelectionManager(_root);

        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);
        Debug.Log("UIManager initialized successfully");
    }

    private void OnEnable() => _inputManager?.Enable();
    private void OnDisable() => _inputManager?.Disable();
    private void OnDestroy()
    {
        _inputManager?.Cleanup();
        _responsiveManager?.Cleanup();
    }

    #endregion

    #region Public Methods

    public void ShowScreen(string screenName) => _screenManager?.ShowScreen(screenName);
    public void SwitchToPlayerInput() => _inputManager?.SwitchToPlayerInput();
    public void SwitchToUIInput() => _inputManager?.SwitchToUIInput();
    public void RefreshResponsiveUI() => _responsiveManager?.RefreshResponsiveUI();

    // Network Events
    public void OnConnectedToServer()
    {
        Debug.Log("Connected to server");
        ShowScreen(UIScreenManager.LobbyListScreenName);
    }

    public void OnDisconnectedFromServer()
    {
        Debug.Log("Disconnected from server");
        ShowScreen(UIScreenManager.MenuScreenName);
        SwitchToUIInput();
    }

    #endregion
}