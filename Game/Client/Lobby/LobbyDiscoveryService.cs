using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class LobbyDiscoveryService : MonoBehaviour
{
    public static LobbyDiscoveryService Instance { get; private set; }

    [Header("Network")]
    public int broadcastPort = 7979;
    public float broadcastInterval = 1f;

    [Header("Local info (server)")]
    public bool advertiseAsServer = false;
    public string serverLobbyName = "MyLobby";
    public int serverMaxPlayers = 9;
    public string serverPassword = "";

    private UdpClient _udp;
    private IPEndPoint _remoteEP;
    private float _timer;

    // discovered lobbies (updated under lock)
    private readonly Dictionary<string, DiscoveredLobby> _lobbies = new();

    // flag to indicate the background receiver wrote new data
    private volatile bool _hasPendingUpdate = false;

    public IReadOnlyList<DiscoveredLobby> Discovered
    {
        get
        {
            lock (_lobbies)
            {
                return new List<DiscoveredLobby>(_lobbies.Values);
            }
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        _udp = new UdpClient();
        _udp.EnableBroadcast = true;
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _remoteEP = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

        StartReceive();
    }

    private void OnDestroy()
    {
        try { _udp?.Close(); _udp?.Dispose(); } catch { }
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        _timer += Time.unscaledDeltaTime;
        if (_timer >= broadcastInterval)
        {
            _timer = 0f;
            if (advertiseAsServer)
                BroadcastServerPresence();
        }

        // If background receiver requested an update, rebuild LatestDiscovered on main thread
        if (_hasPendingUpdate)
        {
            List<LobbyInfo> list;
            lock (_lobbies)
            {
                list = ConvertToLobbyInfoListLocked();
                _hasPendingUpdate = false;
            }

            // publish to static API for UI consumption (main thread)
            LobbyDiscoverySystem.LatestDiscovered = list;
        }
    }

    private async void StartReceive()
    {
        // receive loop on threadpool via async
        while (true)
        {
            try
            {
                var res = await _udp.ReceiveAsync();
                // process datagram on background thread, but only mutate internal _lobbies under lock and set flag
                ProcessDatagramBackground(res.Buffer, res.RemoteEndPoint);
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception e)
            {
                Debug.LogWarning($"Discovery receive error: {e}");
            }
        }
    }

    private void ProcessDatagramBackground(byte[] data, IPEndPoint from)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var info = JsonUtility.FromJson<DiscoveryPacket>(json);
            if (info == null) return;

            string key = $"{from.Address}:{info.port}:{info.lobbyName}";

            lock (_lobbies)
            {
                _lobbies[key] = new DiscoveredLobby
                {
                    Ip = from.Address.ToString(),
                    Port = info.port,
                    Name = info.lobbyName,
                    MaxPlayers = info.maxPlayers,
                    CurrentPlayers = info.currentPlayers,
                    Password = info.password,
                    PasswordProtected = !string.IsNullOrEmpty(info.password),
                    LastSeen = Time.realtimeSinceStartup
                };

                // mark for main-thread publication
                _hasPendingUpdate = true;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed process discovery packet: {e}");
        }
    }

    private List<LobbyInfo> ConvertToLobbyInfoListLocked()
    {
        var list = new List<LobbyInfo>();
        foreach (var kv in _lobbies.Values)
        {
            // drop old
            if (Time.realtimeSinceStartup - kv.LastSeen > 5f) continue;

            list.Add(new LobbyInfo
            {
                Name = kv.Name,
                Password = kv.PasswordProtected ? "●●●" : "",
                IsOpen = !kv.PasswordProtected,
                PlayerCount = kv.CurrentPlayers,
                MaxPlayers = kv.MaxPlayers,
                Ip = kv.Ip,
                Port = kv.Port
            });
        }
        return list;
    }

    private void BroadcastServerPresence()
    {
        try
        {
            var packet = new DiscoveryPacket
            {
                lobbyName = serverLobbyName,
                port = broadcastPort,
                maxPlayers = serverMaxPlayers,
                currentPlayers = 1,
                password = serverPassword
            };

            var json = JsonUtility.ToJson(packet);
            var bytes = Encoding.UTF8.GetBytes(json);
            _udp.Send(bytes, bytes.Length, _remoteEP);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Discovery broadcast error: {e}");
        }
    }

    [Serializable]
    private class DiscoveryPacket
    {
        public string lobbyName;
        public int port;
        public int maxPlayers;
        public int currentPlayers;
        public string password;
    }

    public class DiscoveredLobby
    {
        public string Ip;
        public int Port;
        public string Name;
        public int MaxPlayers;
        public int CurrentPlayers;
        public string Password;
        public bool PasswordProtected;
        public float LastSeen;
    }
}
