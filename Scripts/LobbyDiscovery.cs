using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System;
using Unity.NetCode;
using System.Collections;

/// <summary>
/// Локальное LAN-обнаружение лобби через UDP Broadcast.
/// </summary>
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
    public List<LobbyInfo> DiscoveredLobbies = new();

    private string uniqueId;
    private bool isInitialized = false;
    private bool isHost = false;
    private LobbyInfo currentLobbyInfo;
    private bool _needsLobbyUpdate = false;

    public string UniqueID => uniqueId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        uniqueId = Guid.NewGuid().ToString().Substring(0, 8);
    }

    private void Start() => StartCoroutine(StartDiscovery());

    private void Update()
    {
        if (_needsLobbyUpdate)
        {
            _needsLobbyUpdate = false;
            OnLobbiesUpdated?.Invoke(new List<LobbyInfo>(DiscoveredLobbies));
        }
    }

    private IEnumerator StartDiscovery()
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

            listenThread = new Thread(ListenForBroadcasts) { IsBackground = true };
            listenThread.Start();
            StartCoroutine(SendDiscoveryRequest());

            isInitialized = true;
            Debug.Log($"LobbyDiscovery initialized (port {broadcastPort}, ID={uniqueId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyDiscovery init failed: {e.Message}");
        }
    }

    private void ListenForBroadcasts()
    {
        var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        while (true)
        {
            try
            {
                var data = udpClient.Receive(ref remoteEndPoint);
                var msg = Encoding.UTF8.GetString(data);

                if (msg.StartsWith("LOBBY:"))
                {
                    var info = JsonUtility.FromJson<LobbyInfo>(msg.Substring(6));
                    if (info.uniqueId == uniqueId) continue; // 🔹 игнорируем свои
                    info.ip = remoteEndPoint.Address.ToString();
                    if (info.port == 0) info.port = gamePort;
                    UpdateLobbyList(info);
                    _needsLobbyUpdate = true;
                }
                else if (msg == "DISCOVER" && isHost)
                {
                    BroadcastLobby(currentLobbyInfo);
                }
            }
            catch (Exception e)
            {
                if (e is ThreadAbortException or ObjectDisposedException)
                    break;
                Debug.LogWarning($"Listen error: {e.Message}");
            }
        }
    }

    private void UpdateLobbyList(LobbyInfo newLobby)
    {
        lock (DiscoveredLobbies)
        {
            DiscoveredLobbies.RemoveAll(l => l.uniqueId == newLobby.uniqueId);
            if (newLobby.isOpen || string.IsNullOrEmpty(newLobby.password))
                DiscoveredLobbies.Add(newLobby);
        }
    }

    private IEnumerator SendDiscoveryRequest()
    {
        while (true)
        {
            if (udpClient != null && !isHost)
            {
                try
                {
                    udpClient.Send(Encoding.UTF8.GetBytes("DISCOVER"), 7, broadcastEndPoint);
                }
                catch { }
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

    public void StopHosting() => isHost = false;

    private IEnumerator HostBroadcastLoop()
    {
        while (isHost)
        {
            BroadcastLobby(currentLobbyInfo);
            yield return new WaitForSeconds(broadcastInterval);
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
            Debug.LogWarning($"Broadcast failed: {e.Message}");
        }
    }

    private void OnDestroy()
    {
        if (listenThread != null && listenThread.IsAlive)
            listenThread.Abort();

        udpClient?.Close();
        udpClient = null;
        if (Instance == this) Instance = null;
    }

    public List<LobbyInfo> GetDiscoveredLobbies()
    {
        lock (DiscoveredLobbies)
            return new List<LobbyInfo>(DiscoveredLobbies);
    }

    public void ClearLobbies()
    {
        lock (DiscoveredLobbies)
        {
            DiscoveredLobbies.Clear();
            _needsLobbyUpdate = true;
        }
    }
}