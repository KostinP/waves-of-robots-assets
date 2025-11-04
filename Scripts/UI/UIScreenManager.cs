using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

public class UIScreenManager
{
    public const string MenuScreenName = "menu_screen";
    public const string SettingsScreenName = "settings_screen";
    public const string StatisticsScreenName = "statistics_screen";
    public const string LobbyListScreenName = "lobby_list_screen";
    public const string LobbySettingsScreenName = "lobby_settings_screen";
    public const string GameScreenName = "game_screen";

    private readonly VisualElement _root;
    private string _currentScreen = MenuScreenName;

    // UI Elements
    private Button _btnSingle;
    private Button _btnCreateLobby;
    private Button _btnStatistics;
    private Button _btnQuit;

    public UIScreenManager(VisualElement root)
    {
        _root = root;
        InitializeButtons();
    }

    private void InitializeButtons()
    {
        _btnSingle = _root.Q<Button>("btnSingle");
        _btnCreateLobby = _root.Q<Button>("btnCreateLobby");
        _btnStatistics = _root.Q<Button>("btnStatistics");
        _btnQuit = _root.Q<Button>("btnQuit");

        SetupButtonCallbacks();
    }

    private void SetupButtonCallbacks()
    {
        if (_btnSingle != null) _btnSingle.clicked += OnSinglePlayer;
        if (_btnCreateLobby != null) _btnCreateLobby.clicked += OnCreateLobby;
        if (_btnStatistics != null) _btnStatistics.clicked += OnStatistics;
        if (_btnQuit != null) _btnQuit.clicked += OnQuit;
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
            // Здесь нужно установить фокус на первый элемент
            // Логика фокуса должна быть в UIInputManager
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
        Debug.Log("Creating lobby...");
        ShowScreen(LobbyListScreenName);
    }

    private void OnStatistics()
    {
        Debug.Log("Opening statistics...");
        // ShowScreen(StatisticsScreenName);
    }

    private void OnQuit()
    {
        Debug.Log("Quit button clicked");
        // Логика выхода должна быть в UIManager
    }

    #endregion
}