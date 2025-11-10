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

// Добавьте эту структуру в начало файла, после using statements
public struct ConnectToServerCommand : IComponentData
{
    public FixedString128Bytes ServerIP;
    public ushort ServerPort;
    public FixedString128Bytes PlayerName;
    public FixedString64Bytes Password;
}

public struct LobbyData
{
    public string name;
    public string password;
    public int maxPlayers;
    public bool isOpen;
}

public class LobbyManager : MonoBehaviour
{
    private LobbyDiscovery _discovery;
    private LobbyData _currentLobby;
    private Dictionary<ulong, PlayerData> _players = new();
    private bool _gameStarted = false;

    private void Start()
    {
        _discovery = LobbyDiscovery.Instance;
        if (_discovery == null)
        {
            _discovery = gameObject.AddComponent<LobbyDiscovery>();
        }
        _discovery.OnLobbiesUpdated += OnLobbiesUpdated;
    }

    private World GetServerWorld()
    {
        foreach (var w in World.All)
            if (w.IsCreated && w.IsServer())
                return w;
        return null;
    }

    private World GetClientWorld()
    {
        foreach (var w in World.All)
            if (w.IsCreated && w.IsClient())
                return w;
        return null;
    }

    public void CreateLobby(LobbyData data, PlayerData hostData)
    {
        _currentLobby = data;

        // Создаем серверный мир если его нет
        if (GetServerWorld() == null)
        {
            CreateServer();
        }

        var serverWorld = GetServerWorld();
        if (serverWorld == null)
        {
            Debug.LogError("Failed to create server world!");
            return;
        }

        var em = serverWorld.EntityManager;

        // Очищаем старые лобби данные
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

        // Запускаем широковещание
        var lobbyInfo = new LobbyInfo
        {
            name = _currentLobby.name,
            currentPlayers = 1,
            maxPlayers = _currentLobby.maxPlayers,
            isOpen = _currentLobby.isOpen,
            password = _currentLobby.password,
            ip = GetLocalIPAddress(),
            port = _discovery.gamePort
        };

        _discovery.StartHosting(lobbyInfo);

        UIManager.Instance.OnLobbyListUpdated();
        FindObjectOfType<MainMenuController>()?.OnLobbyCreated();

        Debug.Log($"Lobby created: {data.name}, broadcasting on port {_discovery.broadcastPort}");
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
#else
        // В билде создаем только сервер
        if (GetServerWorld() == null)
        {
            ClientServerBootstrap.CreateServerWorld("ServerWorld");
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
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return "127.0.0.1";
    }

    public void JoinLobby(LobbyInfo lobbyInfo, string playerName, string password = "")
    {
        Debug.Log($"Joining lobby: {lobbyInfo.name} at {lobbyInfo.ip}:{lobbyInfo.port}");

        // Создаем клиентский мир
        CreateClient();

        var clientWorld = GetClientWorld();
        if (clientWorld == null)
        {
            Debug.LogError("Failed to create client world!");
            return;
        }

        var entityManager = clientWorld.EntityManager;

        // Упрощенный подход к подключению - создаем сущность с компонентом NetworkStreamConnection
        var connectionEntity = entityManager.CreateEntity();

        // Добавляем компоненты для подключения
        entityManager.AddComponent<NetworkStreamConnection>(connectionEntity);
        entityManager.AddComponent<NetworkStreamRequestConnect>(connectionEntity);

        // Создаем команду подключения (альтернативный подход)
        var connectEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(connectEntity, new ConnectToServerCommand
        {
            ServerIP = new FixedString128Bytes(lobbyInfo.ip),
            ServerPort = (ushort)lobbyInfo.port,
            PlayerName = new FixedString128Bytes(playerName),
            Password = new FixedString64Bytes(password)
        });

        // Отправляем команду присоединения к лобби
        var joinCmd = entityManager.CreateEntity();
        entityManager.AddComponentData(joinCmd, new JoinLobbyCommand
        {
            PlayerName = new FixedString128Bytes(playerName),
            Password = new FixedString64Bytes(password),
            ConnectionId = 0
        });

        Debug.Log($"Attempting to connect to {lobbyInfo.ip}:{lobbyInfo.port} as {playerName}");

        // Переключаемся на сцену лобби
        StartCoroutine(LoadLobbySceneWithDelay());
    }

    private IEnumerator LoadLobbySceneWithDelay()
    {
        // Даем время на установку подключения
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
                // Загружаем первую доступную сцену
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

    public void DisbandLobby()
    {
        _discovery.StopHosting();
        _discovery.ClearLobbies();
        ShutdownAllNetCodeWorlds();
        SceneManager.LoadScene("MainMenu");
    }

    public void StartGame()
    {
        _gameStarted = true;
        _discovery.StopHosting();
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;
        var em = serverWorld.EntityManager;
        var startEntity = em.CreateEntity();
        em.AddComponent<StartGameCommand>(startEntity);

        // Загружаем игровую сцену
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
        if (serverWorld == null) return 0;
        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(typeof(LobbyPlayerBuffer));
        if (query.IsEmptyIgnoreFilter) return 0;
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
            return _discovery.GetDiscoveredLobbies();
        }
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
                // Запрос пароля для закрытого лобби
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

    private void ShowPasswordPrompt(LobbyInfo lobbyInfo)
    {
        // Упрощенная реализация запроса пароля
        string password = "";

#if UNITY_EDITOR
        // В редакторе используем системный диалог
        //password = UnityEditor.EditorUtility.InputDialog("Password Required", "Enter lobby password:", "");
#else
        // В билде используем упрощенный подход - временный пароль
        Debug.Log("Password protected lobby - using temporary password '123'");
        password = "123";
#endif

        if (!string.IsNullOrEmpty(password))
        {
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
        foreach (var world in World.All)
            if (world.IsCreated && (world.IsServer() || world.IsClient()))
                world.Dispose();
    }

    // Метод для проверки статуса подключения
    public bool IsConnectedToServer()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null) return false;

        var query = clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
        return !query.IsEmptyIgnoreFilter;
    }
}