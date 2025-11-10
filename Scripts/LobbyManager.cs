using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using Unity.Entities;
using Unity.NetCode;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;

public class LobbyManager : MonoBehaviour
{
    private LobbyDiscovery _discovery;
    private LobbyData _currentLobby;
    private Dictionary<ulong, PlayerData> _players = new();
    private bool _gameStarted = false;

    private void Start()
    {
        _discovery = gameObject.AddComponent<LobbyDiscovery>();
        _discovery.OnLobbiesUpdated += OnLobbiesUpdated;
    }

    private World GetServerWorld()
    {
        foreach (var w in World.All)
            if (w.IsCreated && w.IsServer())
                return w;
        return null;
    }

    public void CreateLobby(LobbyData data, PlayerData hostData)
    {
        _currentLobby = data;
        var serverWorld = GetServerWorld();
        if (serverWorld == null) { Debug.LogError("No server world!"); return; }

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

        StartCoroutine(BroadcastLoop());
        UIManager.Instance.OnLobbyListUpdated(); // Обновляем UI
        FindObjectOfType<MainMenuController>()?.OnLobbyCreated();
    }

    IEnumerator BroadcastLoop()
    {
        while (!_gameStarted)
        {
            var info = new LobbyInfo
            {
                name = _currentLobby.name,
                currentPlayers = GetPlayerCount(),
                maxPlayers = _currentLobby.maxPlayers,
                isOpen = _currentLobby.isOpen,
                password = _currentLobby.password
            };
            _discovery.BroadcastLobby(info);
            yield return new WaitForSeconds(2f);
        }
    }

    public void JoinLobby(string ip, string playerName, string password = "")
    {
        World clientWorld = null;
        foreach (var w in World.All)
        {
            if (w.IsCreated && w.IsClient())
            {
                clientWorld = w;
                break;
            }
        }

        if (clientWorld != null)
        {
            var em = clientWorld.EntityManager;
            var joinCmd = em.CreateEntity();
            em.AddComponentData(joinCmd, new JoinLobbyCommand
            {
                PlayerName = new FixedString128Bytes(playerName),
                Password = new FixedString64Bytes(password),
                ConnectionId = 0
            });
        }
        SceneManager.LoadScene("Game");
    }

    public void KickPlayer(ulong connectionId)
    {
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkId>());
        using var entities = query.ToEntityArray(Allocator.Temp);
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
        ShutdownAllNetCodeWorlds();
        SceneManager.LoadScene("ManagersScene");
    }

    public void StartGame()
    {
        _gameStarted = true;
        var serverWorld = GetServerWorld();
        if (serverWorld == null) return;

        var em = serverWorld.EntityManager;
        var startEntity = em.CreateEntity();
        em.AddComponent<StartGameCommand>(startEntity);
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

        var lobbies = _discovery.DiscoveredLobbies;
        if (lobbies == null) return;

        foreach (var lobby in lobbies)
        {
            CreateLobbyItem(scroll, lobby);
        }
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
        var playersLabel = new Label($"{info.currentPlayers}/{info.maxPlayers}");
        var typeLabel = new Label(info.isOpen ? "Открытое" : "Пароль");
        var joinBtn = new Button(() => JoinLobby(info.ip, SettingsManager.Instance.CurrentSettings.playerName, PromptPassword()))
        {
            text = "Join"
        };

        item.Add(nameLabel);
        item.Add(playersLabel);
        item.Add(typeLabel);
        item.Add(joinBtn);

        scroll.Add(item);
        return item;
    }

    private string PromptPassword() => "";

    private VisualElement CreatePlayerItem(ScrollView scroll, ulong id, string name)
    {
        var item = new VisualElement();
        item.AddToClassList("player-item");

        var nameLabel = new Label(name);
        var pingLabel = new Label($"{GetPing(id)} мс");
        var kickBtn = new Button(() => KickPlayer(id)) { text = "Kick" };

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
        using var players = query.ToComponentDataArray<PlayerComponent>(Allocator.Temp);

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
}
