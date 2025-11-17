using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class UIScreenManager
{
    public const string MenuScreenName = "menu_screen";
    public const string LobbyListScreenName = "lobby_list_screen";
    public const string LobbySettingsScreenName = "lobby_settings_screen";
    public const string SettingsScreenName = "settings_screen";

    private readonly VisualElement _root;
    private readonly MainMenuController _controller;

    private string _currentScreen = MenuScreenName;

    private Button _btnShowLobbyList;
    private Button _btnOpenSettings;
    private Button _btnCreateLobby;
    private Button _btnStartGame;
    private Button _btnRefreshLobbyList;
    private ScrollView _lobbyListScroll;
    private ScrollView _playersScroll;

    public UIScreenManager(VisualElement root, MainMenuController controller)
    {
        _root = root;
        _controller = controller;
        InitializeUI();
    }

    private void InitializeUI()
    {
        var menu = _root.Q<VisualElement>(MenuScreenName);
        _btnShowLobbyList = menu.Q<Button>("btnShowLobbyList");
        _btnOpenSettings = menu.Q<Button>("btnSettings");

        var lobbyList = _root.Q<VisualElement>(LobbyListScreenName);
        _btnCreateLobby = lobbyList.Q<Button>("btnCreateLobby");
        _btnRefreshLobbyList = lobbyList.Q<Button>("btnRefreshLobbyList");
        _lobbyListScroll = lobbyList.Q<ScrollView>("lobbyListScroll");

        var lobbySettings = _root.Q<VisualElement>(LobbySettingsScreenName);
        _playersScroll = lobbySettings.Q<ScrollView>("playersScroll");
        _btnStartGame = lobbySettings.Q<Button>("btnStartGame");

        _btnShowLobbyList.clicked += () => ShowScreen(LobbyListScreenName);
        _btnOpenSettings.clicked += () => ShowScreen(SettingsScreenName);
        _btnCreateLobby.clicked += () => _controller.CreateLobby();
        _btnRefreshLobbyList.clicked += () => RefreshLobbyList();
        _btnStartGame.clicked += () => LobbyClientRequests.SendStartGame();
    }

    public void ShowScreen(string screenName)
    {
        var screens = _root.Query<VisualElement>(className: "screen").ToList();
        foreach (var s in screens)
            s.style.display = s.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;

        _currentScreen = screenName;

        if (screenName == LobbyListScreenName)
            RefreshLobbyList();
    }

    // ------------------ Lobby List Update ------------------
    public void RefreshLobbyList()
    {
        _lobbyListScroll.Clear();
        var list = LobbyDiscoverySystem.LatestDiscovered; // New ECS-based discovery
        if (list == null || list.Count == 0)
        {
            _lobbyListScroll.Add(new Label("No lobbies found."));
            return;
        }

        foreach (var lobby in list)
            CreateLobbyItem(_lobbyListScroll, lobby);
    }

    private void CreateLobbyItem(VisualElement root, LobbyInfo info)
    {
        var item = new VisualElement();
        item.AddToClassList("lobby-item");

        item.Add(new Label(info.Name));
        item.Add(new Label($"{info.PlayerCount}/{info.MaxPlayers}"));
        item.Add(new Label(info.IsOpen ? "Open" : "Closed"));

        var btn = new Button(() =>
        {
            _controller.JoinLobby(info.Name, info.Password);
        })
        { text = "Join" };

        item.Add(btn);
        root.Add(item);
    }

    // ---------------- Player List ----------------
    public void PopulatePlayerScroll(List<LobbyPlayerInfo> players)
    {
        _playersScroll.Clear();

        foreach (var p in players)
        {
            var item = new VisualElement();
            item.style.flexDirection = FlexDirection.Row;

            item.Add(new Label(p.Name.ToString()));
            item.Add(new Label($"{p.Ping} ms"));
            item.Add(new Label(p.Weapon.ToString()));

            if (p.IsLocalPlayer == false && p.IsHost)
            {
                var kickBtn = new Button(() => LobbyClientRequests.SendKick(p.ConnectionId));
                kickBtn.text = "Kick";
                item.Add(kickBtn);
            }

            _playersScroll.Add(item);
        }
    }
}
