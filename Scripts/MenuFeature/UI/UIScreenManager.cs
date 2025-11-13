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
    public const string LobbyListScreenName = "lobby_list_screen";
    public const string LobbySettingsScreenName = "lobby_settings_screen";
    public const string SettingsScreenName = "settings_screen";
    private Button _btnRefreshLobbyList;

    private readonly VisualElement _root;
    private readonly MainMenuController _controller;
    private string _currentScreen = MenuScreenName;

    // UI Elements
    private Button _btnShowLobbyList;
    private Button _btnOpenSettings;
    private Button _btnCreateLobby;
    private Button _btnDisbandLobby;
    private Button _btnStartGame;
    private ScrollView _lobbyListScroll;
    private ScrollView _playersScroll;

    private bool _isProcessingDisband = false;

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
            LobbyDiscovery.Instance.OnLobbyClosed += OnLobbyClosed;
        }
    }

    public string GetCurrentScreen() => _currentScreen;

    public void OnLobbyClosed(string lobbyId)
    {
        Debug.Log($"UIScreenManager: Lobby {lobbyId} closed, current screen: {_currentScreen}");

        // ФИКС: Используем UnityMainThreadDispatcher для вызова в главном потоке
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            _controller?.HandleLobbyClosed(lobbyId);
        });
    }

    private void InitializeButtons()
    {
        Debug.Log("Initializing UI buttons...");

        var menuScreen = _root.Q<VisualElement>(MenuScreenName);
        _btnShowLobbyList = menuScreen?.Q<Button>("btnShowLobbyList");
        Debug.Log($"_btnShowLobbyList found: {_btnShowLobbyList != null}");

        _btnOpenSettings = menuScreen?.Q<Button>("btnSettings");
        Debug.Log($"_btnOpenSettings found: {_btnOpenSettings != null}");

        var lobbyListScreen = _root.Q<VisualElement>(LobbyListScreenName);
        _btnCreateLobby = lobbyListScreen?.Q<Button>("btnCreateLobby");
        _btnRefreshLobbyList = lobbyListScreen?.Q<Button>("btnRefreshLobbyList");
        _lobbyListScroll = lobbyListScreen?.Q<ScrollView>("lobbyListScroll");

        Debug.Log($"_btnCreateLobby found: {_btnCreateLobby != null}");
        Debug.Log($"_btnRefreshLobbyList found: {_btnRefreshLobbyList != null}");
        Debug.Log($"_lobbyListScroll found: {_lobbyListScroll != null}");

        var lobbySettingsScreen = _root.Q<VisualElement>(LobbySettingsScreenName);
        _playersScroll = lobbySettingsScreen?.Q<ScrollView>("playersScroll");
        _btnDisbandLobby = lobbySettingsScreen?.Q<Button>("btnDisbandLobby");
        _btnStartGame = lobbySettingsScreen?.Q<Button>("btnStartGame");

        Debug.Log("UI buttons initialization complete");
    }

    private void SetupCallbacks()
    {
        if (_btnShowLobbyList != null)
            _btnShowLobbyList.clicked += () => {
                _controller.ShowScreen(LobbyListScreenName);
                RefreshLobbyList();
            };

        if (_btnOpenSettings != null)
            _btnOpenSettings.clicked += () => {
                _controller.ShowScreen(SettingsScreenName);
            };

        if (_btnCreateLobby != null)
            _btnCreateLobby.clicked += OnCreateLobby;

        if (_btnRefreshLobbyList != null)
            _btnRefreshLobbyList.clicked += () => {
                Debug.Log("Manual lobby list refresh");
                RefreshLobbyList();
                // Принудительно отправляем DISCOVER запрос
                if (LobbyDiscovery.Instance != null)
                {
                    LobbyDiscovery.Instance.ForceDiscovery();
                }
            };

        if (_btnDisbandLobby != null)
            _btnDisbandLobby.clicked += OnDisbandLobby;

        if (_btnStartGame != null)
            _btnStartGame.clicked += () => UIManager.Instance.StartGame();

        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbiesUpdated += (lobbies) => {
                if (_currentScreen == LobbyListScreenName)
                {
                    RefreshLobbyList();
                }
            };
        }

    }

    private void OnLobbiesUpdated(List<LobbyInfo> lobbies)
    {
        Debug.Log($"UIScreenManager: OnLobbiesUpdated called with {lobbies.Count} lobbies");
        Debug.Log($"Current screen: {_currentScreen}, should be: {LobbyListScreenName}");

        // ОБНОВЛЯЕМ В ЛЮБОМ СЛУЧАЕ, ЕСЛИ МЫ НА ЭКРАНЕ СПИСКА ЛОББИ
        if (_currentScreen == LobbyListScreenName)
        {
            Debug.Log("Refreshing lobby list UI because we're on the lobby list screen");
            RefreshLobbyList();
        }
        else
        {
            Debug.Log($"Not refreshing UI because current screen is {_currentScreen}, not {LobbyListScreenName}");
            // НО СОХРАНЯЕМ ДАННЫЕ ДЛЯ БУДУЩЕГО ИСПОЛЬЗОВАНИЯ
        }
    }


    public void ShowScreen(string screenName)
    {
        // Если запрашивают настройки - игнорируем, т.к. это обрабатывается в MainMenuController
        if (screenName == SettingsScreenName) return;

        Debug.Log($"ShowScreen called: {screenName}");

        var screens = _root.Query<VisualElement>(className: "screen").ToList();
        foreach (var screen in screens)
        {
            screen.style.display = screen.name == screenName ? DisplayStyle.Flex : DisplayStyle.None;
        }
        _currentScreen = screenName;

        Debug.Log($"Screen set to: {screenName}");

        if (screenName == LobbyListScreenName)
        {
            RefreshLobbyList();
            if (LobbyDiscovery.Instance != null)
            {
                LobbyDiscovery.Instance.ForceDiscovery();
            }
        }
    }

    public void RefreshLobbyList()
    {
        Debug.Log($"RefreshLobbyList called, current screen: {_currentScreen}, _lobbyListScroll is null: {_lobbyListScroll == null}");

        if (_lobbyListScroll != null)
        {
            _lobbyListScroll.Clear();
            Debug.Log("Cleared lobby list scroll");

            // Получаем lobbies напрямую из LobbyDiscovery
            var lobbies = LobbyDiscovery.Instance?.GetDiscoveredLobbies() ?? new List<LobbyInfo>();
            Debug.Log($"RefreshLobbyList: Got {lobbies.Count} lobbies from LobbyDiscovery");

            if (lobbies.Count == 0)
            {
                var noLobbies = new Label("No lobbies found. Waiting for discovery...");
                noLobbies.AddToClassList("no-lobbies-label");
                _lobbyListScroll.Add(noLobbies);
                Debug.Log("Added 'no lobbies' message");
            }
            else
            {
                foreach (var lobby in lobbies)
                {
                    Debug.Log($"RefreshLobbyList: Adding lobby to UI: {lobby.name}");
                    CreateLobbyItem(_lobbyListScroll, lobby);
                }
                Debug.Log($"Refreshed lobby list with {lobbies.Count} lobbies");
            }
        }
        else
        {
            Debug.LogError("_lobbyListScroll is null - cannot refresh lobby list");
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
            if (info.isOpen || string.IsNullOrEmpty(info.password))
            {
                // Сначала переключаем экран, затем присоединяемся
                _controller.OnJoinedAsClient(); // ← Новый метод
                UIManager.Instance.LobbyManager.JoinLobby(info,
                    SettingsManager.Instance.CurrentSettings.playerName,
                    info.password);
            }
            else
            {
                // Вызываем метод из LobbyManager вместо несуществующего локального метода
                UIManager.Instance.LobbyManager.ShowPasswordPrompt(info);
            }
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
        // Имя игрока берём из SettingsManager — игрок не может менять параметры игры при подключении
        var playerName = SettingsManager.Instance != null ? SettingsManager.Instance.CurrentSettings.playerName : "Player";
        var selectedCharacter = _controller.CharacterSelectionManager.GetSelectedCharacter();
        var playerData = new PlayerData
        {
            name = new FixedString128Bytes(playerName),
            selectedCharacter = selectedCharacter
        };
        UIManager.Instance.LobbyManager.CreateLobby(lobbyData, playerData);
    }

    public void OnDisbandLobby()
    {
        Debug.Log("UIScreenManager: Disbanding lobby...");

        // ФИКС: Добавляем проверку чтобы предотвратить множественные вызовы
        if (_isProcessingDisband)
        {
            Debug.LogWarning("UIScreenManager: Disband already in progress, ignoring");
            return;
        }

        _isProcessingDisband = true;

        try
        {
            var lobbyManager = UIManager.Instance?.LobbyManager;
            if (lobbyManager != null)
            {
                lobbyManager.DisbandLobby();
            }

            // ФИКС: Используем MainMenuController для вызова корутины
            _controller?.ShowLobbyListAfterDisband();
        }
        finally
        {
            _isProcessingDisband = false;
        }
    }

    public void ReturnToMainMenu() => ShowScreen(MenuScreenName);

    public void UpdatePlayerList()
    {
        if (_playersScroll != null)
        {
            _playersScroll.Clear();

            // Получаем ConnectionId локального игрока через UIManager
            var localConnectionId = UIManager.Instance?.GetLocalPlayerConnectionId() ?? 0;

            // 🔹 ИСПРАВЛЕНИЕ: Используем правильный метод
            if (UIManager.Instance?.LobbyManager != null)
            {
                var players = UIManager.Instance.LobbyManager.GetLobbyPlayers();
                foreach (var player in players)
                {
                    CreatePlayerListItem(_playersScroll, player, localConnectionId);
                }
            }
        }
    }

    private void CreatePlayerListItem(ScrollView scroll, LobbyPlayerInfo player, ulong localPlayerConnectionId)
    {
        var item = new VisualElement();
        item.AddToClassList("player-item");

        var nameLabel = new Label(player.Name);
        nameLabel.AddToClassList("player-name");

        var weaponLabel = new Label(player.Weapon);
        weaponLabel.AddToClassList("player-weapon");

        var pingLabel = new Label($"{player.Ping} ms");
        pingLabel.AddToClassList("player-ping");

        item.Add(nameLabel);
        item.Add(weaponLabel);
        item.Add(pingLabel);

        // Добавляем кнопку "Выгнать" только если:
        // 1. Локальный игрок - хост (ConnectionId = 0)
        // 2. Игрок не является самим собой
        if (localPlayerConnectionId == 0 && player.ConnectionId != localPlayerConnectionId)
        {
            var kickBtn = new Button(() => {
                UIManager.Instance?.LobbyManager?.KickPlayer(player.ConnectionId);
            })
            {
                text = "Kick"
            };
            kickBtn.AddToClassList("kick-button");
            item.Add(kickBtn);
        }

        scroll.Add(item);
    }
}