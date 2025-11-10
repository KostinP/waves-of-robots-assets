using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.NetCode;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

public class UIScreenManager
{
    public const string MenuScreenName = "menu_screen";
    public const string SettingsScreenName = "settings_screen";
    public const string LobbyListScreenName = "lobby_list_screen";
    public const string LobbySettingsScreenName = "lobby_settings_screen";

    private readonly VisualElement _root;
    private readonly MainMenuController _controller;
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
        _controller = controller;
        InitializeButtons();
        SetupCallbacks();

        // Подписываемся на обновления списка лобби
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbiesUpdated += OnLobbiesUpdated;
        }
    }

    private void InitializeButtons()
    {
        var menuScreen = _root.Q<VisualElement>(MenuScreenName);
        _btnShowLobbyList = menuScreen?.Q<Button>("btnShowLobbyList");

        var lobbyListScreen = _root.Q<VisualElement>(LobbyListScreenName);
        _btnCreateLobby = lobbyListScreen?.Q<Button>("btnCreateLobby");
        _lobbyListScroll = lobbyListScreen?.Q<ScrollView>("lobbyListScroll");

        var lobbySettingsScreen = _root.Q<VisualElement>(LobbySettingsScreenName);
        _playersScroll = lobbySettingsScreen?.Q<ScrollView>("playersScroll");
        _btnDisbandLobby = lobbySettingsScreen?.Q<Button>("btnDisbandLobby");
        _btnStartGame = lobbySettingsScreen?.Q<Button>("btnStartGame");
    }

    private void SetupCallbacks()
    {
        if (_btnShowLobbyList != null)
            _btnShowLobbyList.clicked += () => {
                _controller.ShowScreen(LobbyListScreenName);
                RefreshLobbyList();
            };

        if (_btnCreateLobby != null)
            _btnCreateLobby.clicked += OnCreateLobby;

        if (_btnDisbandLobby != null)
            _btnDisbandLobby.clicked += OnDisbandLobby;

        if (_btnStartGame != null)
            _btnStartGame.clicked += () => UIManager.Instance.StartGame();
    }

    private void OnLobbiesUpdated(List<LobbyInfo> lobbies)
    {
        if (_currentScreen == LobbyListScreenName)
        {
            RefreshLobbyList();
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

        if (screenName == LobbyListScreenName)
        {
            RefreshLobbyList();
        }

        Debug.Log($"Showing screen: {screenName}");
    }

    public void RefreshLobbyList()
    {
        if (_lobbyListScroll != null)
        {
            _lobbyListScroll.Clear();

            var lobbies = UIManager.Instance.LobbyManager.GetDiscoveredLobbies();
            if (lobbies == null || lobbies.Count == 0)
            {
                var noLobbies = new Label("No lobbies found. Waiting for discovery...");
                noLobbies.AddToClassList("no-lobbies-label");
                _lobbyListScroll.Add(noLobbies);
            }
            else
            {
                foreach (var lobby in lobbies)
                {
                    CreateLobbyItem(_lobbyListScroll, lobby);
                }
                Debug.Log($"Refreshed lobby list with {lobbies.Count} lobbies");
            }
        }
    }

    private VisualElement CreateLobbyItem(ScrollView scroll, LobbyInfo info)
    {
        var item = new VisualElement();
        item.AddToClassList("lobby-item");

        var nameLabel = new Label(info.name);
        nameLabel.AddToClassList("lobby-name");

        var playersLabel = new Label($"{info.currentPlayers}/{info.maxPlayers}");
        playersLabel.AddToClassList("lobby-players");

        var typeLabel = new Label(info.isOpen ? "Open" : "Password");
        typeLabel.AddToClassList("lobby-type");

        var joinBtn = new Button(() => {
            Debug.Log($"Joining lobby: {info.name} at {info.ip}:{info.port}");
            UIManager.Instance.LobbyManager.JoinLobby(info,
                SettingsManager.Instance.CurrentSettings.playerName,
                info.password);
        })
        {
            text = "Join"
        };
        joinBtn.AddToClassList("join-button");

        item.Add(nameLabel);
        item.Add(playersLabel);
        item.Add(typeLabel);
        item.Add(joinBtn);

        scroll.Add(item);
        return item;
    }

    private void OnCreateLobby()
    {
        var lobbyData = _controller.LobbySetupManager.GetLobbyData();
        var playerName = _controller.LobbySetupManager.GetPlayerName();
        var selectedCharacter = _controller.CharacterSelectionManager.GetSelectedCharacter();
        var playerData = new PlayerData
        {
            name = new FixedString128Bytes(playerName),
            selectedCharacter = selectedCharacter
        };
        UIManager.Instance.LobbyManager.CreateLobby(lobbyData, playerData);
    }

    private void OnDisbandLobby()
    {
        UIManager.Instance.LobbyManager.DisbandLobby();
    }

    public void ReturnToMainMenu() => ShowScreen(MenuScreenName);
    public string GetCurrentScreen() => _currentScreen;

    public void UpdatePlayerList()
    {
        if (_playersScroll != null)
        {
            _playersScroll.Clear();
            UIManager.Instance.LobbyManager.PopulatePlayerList(_playersScroll);
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