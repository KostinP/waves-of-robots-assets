using UnityEngine;
using UnityEngine.UIElements;

public class MainMenuController : MonoBehaviour
{
    private VisualElement _root;
    private UIInputManager _inputManager;
    private UIScreenManager _screenManager;
    private UILobbySetupManager _lobbySetup;
    private UICharacterSelectionManager _charSelect;
    private UISettingsManager _settings;

    public UILobbySetupManager LobbySetupManager => _lobbySetup;
    public UICharacterSelectionManager CharacterSelectionManager => _charSelect;

    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        _root = uiDoc.rootVisualElement;

        _inputManager = new UIInputManager(UIManager.Instance.InputActions, _root, this);

        _screenManager = new UIScreenManager(_root, this);  // ← this!
        _lobbySetup = new UILobbySetupManager(_root, this);
        _charSelect = new UICharacterSelectionManager(_root, this);
        _settings = new UISettingsManager(_root, uiDoc, this);

        _inputManager.Enable();
        _screenManager.ShowScreen(UIScreenManager.MenuScreenName);

        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.RefreshForNewScene();
            LocalizationManager.Instance.UpdateAllUIElements();
        }
    }

    public void ShowScreen(string screenName) => _screenManager.ShowScreen(screenName);
    public void UpdatePlayerList() => _screenManager.UpdatePlayerList();
    public void OnLobbyCreated() => ShowScreen(UIScreenManager.LobbySettingsScreenName);
    public void OnLobbyListUpdated() => _screenManager.RefreshLobbyList();

    private void OnDestroy()
    {
        _inputManager?.Cleanup();
        _settings?.Cleanup();
    }
}