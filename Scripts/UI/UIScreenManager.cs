using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;

public class UIScreenManager
{
    public const string MenuScreenName = "menu_screen";
    public const string SettingsScreenName = "settings_screen";
    public const string LobbyListScreenName = "lobby_list_screen";
    public const string LobbySettingsScreenName = "lobby_settings_screen";
    public const string GameScreenName = "game_screen";

    private readonly VisualElement _root;
    private string _currentScreen = MenuScreenName;
    private LobbySettingsManager _lobbySettingsManager;

    // UI Elements
    private Button _btnSingle;
    private Button _btnCreateLobby;
    private Button _btnQuit;
    private Button _btnDisbandLobby;
    private Button _btnCreateLobbyConfirm;

    public UIScreenManager(VisualElement root)
    {
        _root = root;
        _lobbySettingsManager = new LobbySettingsManager(root);
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        _btnSingle = _root.Q<Button>("btnSingle");
        _btnCreateLobby = _root.Q<Button>("btnShowLobbyList");
        _btnQuit = _root.Q<Button>("btnQuit");
        _btnDisbandLobby = _root.Q<Button>("btnDisbandLobby");
        _btnCreateLobbyConfirm = _root.Q<Button>("btnCreateLobby");

        SetupButtonCallbacks();
    }

    private void SetupButtonCallbacks()
    {
        if (_btnSingle != null) _btnSingle.clicked += OnSinglePlayer;
        if (_btnCreateLobby != null) _btnCreateLobby.clicked += OnCreateLobby;
        if (_btnQuit != null) _btnQuit.clicked += OnQuit;
        if (_btnDisbandLobby != null) _btnDisbandLobby.clicked += OnDisbandLobby;
        if (_btnCreateLobbyConfirm != null) _btnCreateLobbyConfirm.clicked += OnConfirmCreateLobby; // ← НОВЫЙ ХЕНДЛЕР
    }

    private void OnConfirmCreateLobby()
    {
        Debug.Log("Lobby created! Opening settings...");
        _lobbySettingsManager.SyncAllSettings();
        ShowScreen(LobbySettingsScreenName);
    }

    public void ShowScreen(string screenName)
    {
        var screens = _root.Query<VisualElement>(className: "screen").ToList();
        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }
        _currentScreen = screenName;
        Debug.Log($"Showing screen: {screenName}");
    }

    public void ReturnToMainMenu() => ShowScreen(MenuScreenName);
    public string GetCurrentScreen() => _currentScreen;

    public IEnumerator SetInitialFocus(List<VisualElement> interactiveElements)
    {
        yield return null;
        if (interactiveElements.Count > 0)
        {
            interactiveElements[0].Focus();
        }
    }

    #region Button Handlers
    private void OnSinglePlayer()
    {
        Debug.Log("Starting single player...");
        ShowScreen(MenuScreenName);
    }

    private void OnCreateLobby()
    {
        Debug.Log("Opening lobby list...");
        ShowScreen(LobbyListScreenName);
    }

    private void OnQuit()
    {
        Debug.Log("Quit button clicked");
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDisbandLobby()
    {
        Debug.Log("Disbanding lobby...");
        ShowScreen(LobbyListScreenName); // или MenuScreenName
    }
    #endregion
}