using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;
using Unity.NetCode;
using System.Collections;

[System.Serializable]
public struct LobbyInfo
{
    public string name;
    public int currentPlayers;
    public int maxPlayers;
    public bool isOpen;
    public string password;
    public string ip;
    public int port;
    public string uniqueId;
}

public class LobbyDiscovery : MonoBehaviour
{
    public static LobbyDiscovery Instance { get; private set; }

    public int broadcastPort = 8888;
    public int gamePort = 7777;
    public float broadcastInterval = 2f;
    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint;
    private Thread listenThread;
    public Action<List<LobbyInfo>> OnLobbiesUpdated;
    public List<LobbyInfo> DiscoveredLobbies = new List<LobbyInfo>();

    private string uniqueId;
    private bool isInitialized = false;
    private bool isHost = false;
    private LobbyInfo currentLobbyInfo;
    private bool _needsLobbyUpdate = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        uniqueId = System.Guid.NewGuid().ToString().Substring(0, 8);
    }

    void Start()
    {
        StartCoroutine(StartDiscovery());
    }

    void Update()
    {
        if (_needsLobbyUpdate)
        {
            _needsLobbyUpdate = false;
            OnLobbiesUpdated?.Invoke(new List<LobbyInfo>(DiscoveredLobbies));
        }
    }

    IEnumerator StartDiscovery()
    {
        yield return new WaitForSeconds(1f);

        if (isInitialized) yield break;

        try
        {
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, broadcastPort));

            udpClient.EnableBroadcast = true;
            broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, broadcastPort);

            listenThread = new Thread(ListenForBroadcasts);
            listenThread.IsBackground = true;
            listenThread.Start();

            StartCoroutine(SendDiscoveryRequest());

            isInitialized = true;
            Debug.Log($"LobbyDiscovery initialized. Listening on port {broadcastPort}, UniqueID: {uniqueId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize LobbyDiscovery: {e.Message}");
        }
    }

    void ListenForBroadcasts()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);

                if (message.StartsWith("LOBBY:"))
                {
                    var info = JsonUtility.FromJson<LobbyInfo>(message.Substring(6));

                    if (info.uniqueId == uniqueId)
                    {
                        continue;
                    }

                    info.ip = remoteEndPoint.Address.ToString();
                    if (info.port == 0) info.port = gamePort;

                    UpdateLobbyList(info);

                    // Обновляем UI в главном потоке
                    _needsLobbyUpdate = true;
                }
                else if (message == "DISCOVER")
                {
                    if (isHost)
                    {
                        BroadcastLobby(currentLobbyInfo);
                    }
                }
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException || e is ObjectDisposedException)
                    break;
                Debug.LogWarning($"Exception in ListenForBroadcasts: {e.Message}");
            }
        }
    }

    void UpdateLobbyList(LobbyInfo newLobby)
    {
        lock (DiscoveredLobbies)
        {
            DiscoveredLobbies.RemoveAll(l => l.uniqueId == newLobby.uniqueId);

            if (newLobby.isOpen || string.IsNullOrEmpty(newLobby.password))
            {
                DiscoveredLobbies.Add(newLobby);
                Debug.Log($"Discovered lobby: {newLobby.name} from {newLobby.ip}:{newLobby.port}");
            }
        }
    }

    IEnumerator SendDiscoveryRequest()
    {
        while (true)
        {
            if (udpClient != null && !isHost)
            {
                try
                {
                    udpClient.Send(Encoding.UTF8.GetBytes("DISCOVER"), 7, broadcastEndPoint);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Failed to send discovery: {e.Message}");
                }
            }
            yield return new WaitForSeconds(3f);
        }
    }

    public void StartHosting(LobbyInfo info)
    {
        isHost = true;
        currentLobbyInfo = info;
        currentLobbyInfo.uniqueId = uniqueId;
        currentLobbyInfo.port = gamePort;

        Debug.Log($"Started hosting lobby: {info.name} on port {gamePort}");

        BroadcastLobby(currentLobbyInfo);
        StartCoroutine(HostBroadcastLoop());
    }

    public void StopHosting()
    {
        isHost = false;
        Debug.Log("Stopped hosting lobby");
    }

    IEnumerator HostBroadcastLoop()
    {
        while (isHost)
        {
            BroadcastLobby(currentLobbyInfo);
            yield return new WaitForSeconds(2f);
        }
    }

    public void BroadcastLobby(LobbyInfo info)
    {
        if (udpClient == null || !isHost) return;

        try
        {
            string message = "LOBBY:" + JsonUtility.ToJson(info);
            byte[] data = Encoding.UTF8.GetBytes(message);
            udpClient.Send(data, data.Length, broadcastEndPoint);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to broadcast lobby: {e.Message}");
        }
    }

    void OnDestroy()
    {
        if (listenThread != null && listenThread.IsAlive)
        {
            listenThread.Abort();
        }

        udpClient?.Close();
        udpClient = null;

        if (Instance == this)
            Instance = null;
    }

    public List<LobbyInfo> GetDiscoveredLobbies()
    {
        lock (DiscoveredLobbies)
        {
            return new List<LobbyInfo>(DiscoveredLobbies);
        }
    }

    public void ClearLobbies()
    {
        lock (DiscoveredLobbies)
        {
            DiscoveredLobbies.Clear();
            _needsLobbyUpdate = true;
        }
    }

    public void RefreshLobbyListUI()
    {
        _needsLobbyUpdate = true;
    }
}