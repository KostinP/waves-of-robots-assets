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

    private readonly VisualElement _root;
    private readonly MainMenuController _controller; // ← Обязательно!
    private string _currentScreen = MenuScreenName;

    // UI Elements
    private Button _btnShowLobbyList;
    private Button _btnCreateLobby;
    private Button _btnDisbandLobby;
    private Button _btnStartGame;
    private ScrollView _lobbyListScroll;
    private ScrollView _playersScroll;

    public UIScreenManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller; // ← Получаем через конструктор
        InitializeButtons();
        SetupCallbacks();
    }

    private void InitializeButtons()
    {
        // Menu screen
        var menuScreen = _root.Q<VisualElement>(MenuScreenName);
        _btnShowLobbyList = menuScreen?.Q<Button>("btnShowLobbyList");

        // Lobby list screen
        var lobbyListScreen = _root.Q<VisualElement>(LobbyListScreenName);
        _btnCreateLobby = lobbyListScreen?.Q<Button>("btnCreateLobby");
        _lobbyListScroll = lobbyListScreen?.Q<ScrollView>("lobbyListScroll");

        // Lobby settings screen
        var lobbySettingsScreen = _root.Q<VisualElement>(LobbySettingsScreenName);
        _playersScroll = lobbySettingsScreen?.Q<ScrollView>("playersScroll");
        _btnDisbandLobby = lobbySettingsScreen?.Q<Button>("btnDisbandLobby");
        _btnStartGame = lobbySettingsScreen?.Q<Button>("btnStartGame");
    }

    private void SetupCallbacks()
    {
        // Lobby List → Lobby List Screen
        if (_btnShowLobbyList != null)
            _btnShowLobbyList.clicked += () => _controller.ShowScreen(LobbyListScreenName);

        // Create Lobby
        if (_btnCreateLobby != null)
            _btnCreateLobby.clicked += OnCreateLobby;

        // Disband Lobby
        if (_btnDisbandLobby != null)
            _btnDisbandLobby.clicked += OnDisbandLobby;

        // Start Game
        if (_btnStartGame != null)
            _btnStartGame.clicked += () => UIManager.Instance.StartGame();
    }

    private void OnCreateLobby()
    {
        // ✅ Используем локальный доступ через _controller
        var lobbyData = _controller.LobbySetupManager.GetLobbyData();
        var playerData = _controller.CharacterSelectionManager.GetPlayerData();

        UIManager.Instance.LobbyManager.CreateLobby(lobbyData, playerData);
    }

    private void OnDisbandLobby()
    {
        UIManager.Instance.LobbyManager.DisbandLobby();
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

    // ✅ Обновление списков через контроллер
    public void UpdatePlayerList()
    {
        // Очищаем и заполняем список игроков
        if (_playersScroll != null)
        {
            _playersScroll.Clear();
            UIManager.Instance.LobbyManager.PopulatePlayerList(_playersScroll);
        }
    }

    public void RefreshLobbyList()
    {
        // Очищаем и заполняем список лобби
        if (_lobbyListScroll != null)
        {
            _lobbyListScroll.Clear();
            UIManager.Instance.LobbyManager.PopulateLobbyList(_lobbyListScroll);
        }
    }

    public IEnumerator SetInitialFocus(List<VisualElement> interactiveElements)
    {
        yield return null;
        if (interactiveElements.Count > 0)
        {
            interactiveElements[0].Focus();
        }
    }
}