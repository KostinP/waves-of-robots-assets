using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using System.Net;
using System.Net.Sockets;
using System;

/// <summary>
/// Управляет локальным лобби: создает серверный мир, запускает discovery broadcast через LobbyDiscovery,
/// отслеживает текущее число игроков и — при достижении maxPlayers — помечает лобби скрытым и стартует игру.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    private LobbyDiscovery _discovery;
    private LobbyData? _currentLobby;
    private LobbyInfo? _currentLobbyInfo;
    private Dictionary<ulong, PlayerData> _players = new();
    private bool _gameStarted = false;
    private Coroutine _lobbyMonitorCoroutine;

    private void Start()
    {
        _discovery = LobbyDiscovery.Instance;
        if (_discovery == null)
        {
            _discovery = gameObject.AddComponent<LobbyDiscovery>();
        }
        _discovery.OnLobbiesUpdated += OnLobbiesUpdated;

        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbyClosed += OnLobbyClosed;
        }
    }

    private void OnLobbyClosed(string lobbyId)
    {
        Debug.Log($"LobbyManager: Lobby {lobbyId} was closed by host");

        // ФИКС: Используем корутину для безопасной проверки в главном потоке
        StartCoroutine(CheckAndHandleLobbyClose(lobbyId));
    }

    private IEnumerator CheckAndHandleLobbyClose(string lobbyId)
    {
        yield return null; // Ждем главный поток

        // Проверяем, находимся ли мы в клиентском режиме
        var clientWorld = GetClientWorld();
        bool isClient = clientWorld != null && clientWorld.IsCreated && clientWorld.IsClient();
        bool isServer = GetServerWorld() != null;

        // Получаем IP текущего подключения для проверки
        string currentLobbyIp = PlayerPrefs.GetString("JoiningLobbyIP", "");
        string currentLobbyId = ""; // Можно добавить сохранение ID лобби

        Debug.Log($"OnLobbyClosed: isClient={isClient}, isServer={isServer}, currentLobbyIp={currentLobbyIp}, closedLobbyId={lobbyId}");

        // ФИКС: Упрощаем логику - если мы клиент (не хост), то выходим
        if (isClient && !isServer)
        {
            Debug.Log("We are a client and host closed the lobby, returning to lobby list");

            // Закрываем клиентское соединение
            if (clientWorld != null && clientWorld.IsCreated)
            {
                try
                {
                    // Находим и закрываем соединение
                    var em = clientWorld.EntityManager;
                    var query = em.CreateEntityQuery(typeof(NetworkStreamConnection));
                    if (!query.IsEmptyIgnoreFilter)
                    {
                        var connectionEntity = query.GetSingletonEntity();
                        em.DestroyEntity(connectionEntity);
                        Debug.Log("Destroyed client connection entity");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Error closing client connection: {e.Message}");
                }
            }

            // Уничтожаем клиентский мир
            ShutdownClientWorld();

            // Очищаем сохраненные данные о подключении
            PlayerPrefs.DeleteKey("JoiningLobbyIP");
            PlayerPrefs.DeleteKey("JoiningLobbyPort");
            PlayerPrefs.Save();

            // Возвращаемся к списку лобби
            StartCoroutine(ReturnToLobbyListWithDelay());
        }
        else
        {
            Debug.Log($"OnLobbyClosed: Not affected - isClient={isClient}, isServer={isServer}");
        }
    }

    private IEnumerator ReturnToLobbyListWithDelay()
    {
        yield return new WaitForSeconds(0.5f); // Даем время на очистку

        // ФИКС: Используем правильный метод возврата
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ReturnToLobbyList();
        }
        else
        {
            // Fallback
            var mainMenuController = FindObjectOfType<MainMenuController>();
            if (mainMenuController != null)
            {
                mainMenuController.ReturnToLobbyList();
            }
        }
    }

    private void ShutdownClientWorld()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld != null && clientWorld.IsCreated)
        {
            try
            {
                clientWorld.Dispose();
                Debug.Log("Client world disposed");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Error disposing client world: {e.Message}");
            }
        }
    }



    private World GetServerWorld()
    {
        foreach (var w in World.All)
        {
            if (w.IsCreated && w.IsServer())
                return w;
        }
        return null;
    }

    private World GetClientWorld()
    {
        foreach (var w in World.All)
        {
            if (w.IsCreated && w.IsClient())
                return w;
        }
        return null;
    }

    public void CreateLobby(LobbyData data, PlayerData hostData)
    {
        _currentLobby = data;

        if (GetServerWorld() == null)
            CreateServer();

        var serverWorld = GetServerWorld();
        if (serverWorld == null)
        {
            Debug.LogError("Failed to create server world!");
            return;
        }

        var em = serverWorld.EntityManager;

        var oldQuery = em.CreateEntityQuery(typeof(LobbyDataComponent));
        if (!oldQuery.IsEmptyIgnoreFilter)
            em.DestroyEntity(oldQuery.GetSingletonEntity());

        var lobbyEntity = em.CreateEntity();
        em.AddComponentData(lobbyEntity, new LobbyDataComponent
        {
            Name = new FixedString128Bytes(data.name),
            Password = new FixedString64Bytes(data.password),
            MaxPlayers = data.maxPlayers,
            IsOpen = data.isOpen
        });

        var buffer = em.AddBuffer<LobbyPlayerBuffer>(lobbyEntity);
        _players.Clear();
        _players[0] = hostData;
        buffer.Add(new LobbyPlayerBuffer
        {
            PlayerName = new FixedString128Bytes(hostData.name),
            ConnectionId = 0
        });

        _currentLobbyInfo = new LobbyInfo
        {
            name = data.name,
            currentPlayers = 1,
            maxPlayers = data.maxPlayers,
            isOpen = data.isOpen,
            password = data.password,
            ip = GetLocalIPAddress(),
            port = _discovery.gamePort,
            uniqueId = _discovery.UniqueID
        };

        _discovery.StartHosting(_currentLobbyInfo.Value);

        if (_lobbyMonitorCoroutine != null) StopCoroutine(_lobbyMonitorCoroutine);
        _lobbyMonitorCoroutine = StartCoroutine(LobbyBroadcastAndMonitorLoop());

        UIManager.Instance.OnLobbyListUpdated();
        UIManager.Instance.OnLobbyCreated();
        UIManager.Instance.OnPlayersUpdated();

        Debug.Log($"Lobby created: {data.name}, broadcasting on port {_discovery.broadcastPort}");
    }

    private IEnumerator LobbyBroadcastAndMonitorLoop()
    {
        // Проверяем, что лобби существует
        if (!_currentLobbyInfo.HasValue) yield break;

        // Периодически обновляем поле currentPlayers и шлём broadcast
        while (!_gameStarted)
        {
            int count = GetPlayerCount();
            var currentInfo = _currentLobbyInfo.Value;
            currentInfo.currentPlayers = Mathf.Max(1, count); // >=1 (хост)
            // Если стали меньше max — оставляем isOpen как есть (обычно true), если достигли — закрываем
            if (currentInfo.currentPlayers >= currentInfo.maxPlayers)
            {
                // Пометим как закрытое — чтобы клиенты не пытались зайти
                currentInfo.isOpen = false;
                _currentLobbyInfo = currentInfo;
                _discovery.BroadcastLobby(currentInfo); // одноразовое обновление со статусом закрыто
                Debug.Log("Lobby full — broadcasting hide/closed and starting game.");
                // Небольшая задержка, чтобы клиенты успели получить сообщение
                yield return new WaitForSeconds(0.5f);
                StartGame(); // автоматически стартуем игру
                yield break;
            }
            else
            {
                // Обычная регулярная рассылка актуального состояния (имя/кол-во/порт)
                if (_currentLobby.HasValue)
                {
                    currentInfo.isOpen = _currentLobby.Value.isOpen;
                }
                _currentLobbyInfo = currentInfo;
                _discovery.BroadcastLobby(currentInfo);
            }

            yield return new WaitForSeconds(1.0f);
        }
    }

    private void CreateServer()
    {
#if UNITY_EDITOR
        // В редакторе создаем и сервер и клиент
        if (World.All.Count == 0)
        {
            var server = ClientServerBootstrap.CreateServerWorld("ServerWorld");
            var client = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }
#endif
    }

    private void CreateClient()
    {
        if (GetClientWorld() == null)
        {
            ClientServerBootstrap.CreateClientWorld("ClientWorld");
        }
    }

    private string GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    Debug.Log($"Found local IP: {ip}");
                    return ip.ToString();
                }
            }

            // Если не нашли подходящий IP, используем локальный
            Debug.LogWarning("No suitable local IP found, using 127.0.0.1");
            return "127.0.0.1";
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error getting local IP: {ex.Message}");
            return "127.0.0.1";
        }
    }

    public void JoinLobby(LobbyInfo lobbyInfo, string playerName, string password = "")
    {
        Debug.Log($"Joining lobby: {lobbyInfo.name} at {lobbyInfo.ip}:{lobbyInfo.port}");

        PlayerPrefs.SetString("JoiningLobbyIP", lobbyInfo.ip);
        PlayerPrefs.SetInt("JoiningLobbyPort", lobbyInfo.port);
        PlayerPrefs.SetString("JoiningPlayerName", playerName);
        PlayerPrefs.Save();

        var mainMenuController = FindObjectOfType<MainMenuController>();
        if (mainMenuController != null)
            mainMenuController.OnJoinedAsClient();
        else
            SceneManager.LoadScene("LobbyScene");

        // получаем строковое название оружия по индексу
        string weaponName = GetWeaponNameFromIndex(SettingsManager.Instance.CurrentSettings.defaultWeaponIndex);

        // отправляем JoinLobbyCommand
        var clientWorld = GetClientWorld();
        if (clientWorld != null)
        {
            var em = clientWorld.EntityManager;
            var req = em.CreateEntity();
            em.AddComponentData(req, new JoinLobbyCommand
            {
                PlayerName = new FixedString128Bytes(playerName),
                Weapon = new FixedString64Bytes(weaponName),
                Password = new FixedString64Bytes(password ?? "")
            });
            em.AddComponentData(req, new SendRpcCommandRequest { TargetConnection = Entity.Null });
        }

        UIManager.Instance.OnLobbyListUpdated();
    }

    private string GetWeaponNameFromIndex(int index)
    {
        switch (index)
        {
            case 0: return "Termit\"Flamethrower\"";
            case 1: return "Titan \"Autocannon\"";
            case 2: return "Volt \"Stun Gun\"";
            case 3: return "Vikhr \"Twin Machine Gun\"";
            default: return $"Weapon{index}";
        }
    }

    private IEnumerator LoadLobbySceneWithDelay()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            if (Application.CanStreamedLevelBeLoaded("LobbyScene"))
            {
                SceneManager.LoadScene("LobbyScene");
            }
            else if (Application.CanStreamedLevelBeLoaded("GameCoreScene"))
            {
                SceneManager.LoadScene("GameCoreScene");
            }
            else
            {
                SceneManager.LoadScene(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene: {e.Message}");
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void KickPlayer(ulong connectionId)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        using var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp);
        foreach (var e in entities)
        {
            if (em.GetComponentData<NetworkId>(e).Value == (int)connectionId)
            {
                em.DestroyEntity(e);
                break;
            }
        }
        _players.Remove(connectionId);
        UIManager.Instance.OnPlayerLeft((int)connectionId);
    }

    private bool _isDisbanding = false; // Добавляем флаг

    public void DisbandLobby()
    {
        if (_isDisbanding)
        {
            Debug.LogWarning("LobbyManager: DisbandLobby already in progress, ignoring duplicate call");
            return;
        }

        _isDisbanding = true;
        Debug.Log("LobbyManager: Starting lobby disband process...");

        try
        {
            // Останавливаем корутины
            if (_lobbyMonitorCoroutine != null)
            {
                Debug.Log("Stopping lobby monitor coroutine...");
                StopCoroutine(_lobbyMonitorCoroutine);
                _lobbyMonitorCoroutine = null;
            }

            // ФИКС: Сначала уведомляем о закрытии
            if (LobbyDiscovery.Instance != null)
            {
                Debug.Log("Sending lobby close notification...");
                LobbyDiscovery.Instance.StopHostingAndNotify();
            }

            // Очищаем данные лобби
            Debug.Log("Clearing lobby data...");
            _players.Clear();
            _gameStarted = false;
            _currentLobby = null;
            _currentLobbyInfo = null;

            // Уничтожаем NetCode миры
            Debug.Log("Shutting down NetCode worlds...");
            ShutdownAllNetCodeWorlds();

            // Останавливаем хостинг в discovery
            if (_discovery != null)
            {
                Debug.Log("Stopping discovery hosting...");
                _discovery.StopHosting();
            }

            // Возвращаем в главное меню
            Debug.Log("Returning to main menu...");
            StartCoroutine(ReturnToMainMenuWithDelay());

            Debug.Log("Lobby disbanded successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error during lobby disband: {e.Message}\n{e.StackTrace}");
            // Все равно пытаемся вернуться в главное меню
            StartCoroutine(ReturnToMainMenuWithDelay());
        }
        finally
        {
            _isDisbanding = false;
        }
    }

    private IEnumerator ReturnToMainMenuWithDelay()
    {
        yield return new WaitForSeconds(0.5f); // Даем время на очистку сетевых соединений

        if (UIManager.Instance != null)
        {
            UIManager.Instance.LeaveGame();
        }
        else
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void StartGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        // Остановим broadcasting — discovery сам прекратит, когда мы вызовем StopHosting
        _discovery.StopHosting();

        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var startEntity = em.CreateEntity();
        em.AddComponentData(startEntity, new StartGameCommand { });

        StartCoroutine(LoadGameSceneWithDelay());
    }

    private IEnumerator LoadGameSceneWithDelay()
    {
        yield return new WaitForSeconds(1f);

        try
        {
            if (Application.CanStreamedLevelBeLoaded("GameCoreScene"))
            {
                SceneManager.LoadScene("GameCoreScene");
            }
            else
            {
                Debug.LogWarning("GameCoreScene not found, loading first available scene");
                SceneManager.LoadScene(0);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game scene: {e.Message}");
            SceneManager.LoadScene("MainMenu");
        }
    }

    private int GetPlayerCount()
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return 1; // хотя бы хост
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(LobbyPlayerBuffer));
        if (query.IsEmptyIgnoreFilter) return 1;
        return em.GetBuffer<LobbyPlayerBuffer>(query.GetSingletonEntity()).Length;
    }

    public void PopulateLobbyList(ScrollView scroll)
    {
        scroll.Clear();
        var lobbies = GetDiscoveredLobbies();
        if (lobbies == null || lobbies.Count == 0)
        {
            var noLobbies = new Label("No lobbies found");
            noLobbies.AddToClassList("no-lobbies-label");
            scroll.Add(noLobbies);
            return;
        }

        foreach (var lobby in lobbies)
        {
            CreateLobbyItem(scroll, lobby);
        }
    }

    public List<LobbyInfo> GetDiscoveredLobbies()
    {
        if (_discovery != null)
        {
            var lobbies = _discovery.GetDiscoveredLobbies();
            Debug.Log($"GetDiscoveredLobbies: returning {lobbies.Count} lobbies");
            foreach (var lobby in lobbies)
            {
                Debug.Log($" - {lobby.name} ({lobby.ip}:{lobby.port}), Open: {lobby.isOpen}");
            }
            return lobbies;
        }
        Debug.LogWarning("GetDiscoveredLobbies: _discovery is null");
        return new List<LobbyInfo>();
    }

    public void PopulatePlayerList(ScrollView scroll)
    {
        scroll.Clear();
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<LobbyPlayerBuffer>());
        if (query.IsEmptyIgnoreFilter) return;
        var buffer = em.GetBuffer<LobbyPlayerBuffer>(query.GetSingletonEntity());
        for (int i = 0; i < buffer.Length; i++)
        {
            var p = buffer[i];
            CreatePlayerItem(scroll, p.ConnectionId, p.PlayerName.ToString());
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
                JoinLobby(info, SettingsManager.Instance.CurrentSettings.playerName, info.password);
            }
            else
            {
                ShowPasswordPrompt(info);
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

    public void ShowPasswordPrompt(LobbyInfo lobbyInfo)
    {
        string password = "";

#if UNITY_EDITOR
        // В редакторе используем системный диалог — оставляем упрощение
#else
        Debug.Log("Password protected lobby - using temporary password '123'");
        password = "123";
#endif

        if (!string.IsNullOrEmpty(password))
        {
            var mainMenuController = FindObjectOfType<MainMenuController>();
            if (mainMenuController != null)
            {
                mainMenuController.OnJoinedAsClient();
            }

            JoinLobby(lobbyInfo, SettingsManager.Instance.CurrentSettings.playerName, password);
        }
    }

    private VisualElement CreatePlayerItem(ScrollView scroll, ulong id, string name)
    {
        var item = new VisualElement();
        item.AddToClassList("player-item");

        var nameLabel = new Label(name);
        nameLabel.AddToClassList("player-name");

        var pingLabel = new Label($"{GetPing(id)} ms");
        pingLabel.AddToClassList("player-ping");

        var kickBtn = new Button(() => KickPlayer(id))
        {
            text = "Kick"
        };
        kickBtn.AddToClassList("kick-button");

        item.Add(nameLabel);
        item.Add(pingLabel);
        item.Add(kickBtn);

        scroll.Add(item);
        return item;
    }

    string GetPing(ulong id)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return "0";
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        using var players = query.ToComponentDataArray<PlayerComponent>(Unity.Collections.Allocator.Temp);
        foreach (var player in players)
        {
            if (player.ConnectionId == id)
                return player.Ping.ToString();
        }
        return "0";
    }

    private void OnLobbiesUpdated(List<LobbyInfo> lobbies)
    {
        UIManager.Instance.OnLobbyListUpdated();
    }

    private void ShutdownAllNetCodeWorlds()
    {
        // СОЗДАЕМ КОПИЮ СПИСКА МИРОВ ДЛЯ БЕЗОПАСНОЙ ИТЕРАЦИИ
        var worldsToDispose = new List<World>();
        foreach (var w in World.All)
        {
            if (w.IsCreated && (w.IsServer() || w.IsClient() || w.IsThinClient()))
                worldsToDispose.Add(w);
        }

        // ТЕПЕРЬ БЕЗОПАСНО УНИЧТОЖАЕМ МИРЫ
        foreach (var w in worldsToDispose)
        {
            try
            {
                w.Dispose();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to dispose world {w.Name}: {e.Message}");
            }
        }
    }

    public bool IsConnectedToServer()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null) return false;

        var query = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
        return !query.IsEmptyIgnoreFilter;
    }

    private void OnDestroy()
    {
        if (LobbyDiscovery.Instance != null)
        {
            LobbyDiscovery.Instance.OnLobbyClosed -= OnLobbyClosed;
        }
    }

}
