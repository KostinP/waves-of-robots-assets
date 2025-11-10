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
}

public class LobbyDiscovery : MonoBehaviour
{
    public static LobbyDiscovery Instance { get; private set; }

    public int port = 8888;
    public float broadcastInterval = 2f;
    private UdpClient udpClient;
    private IPEndPoint broadcastEndPoint;
    private Thread listenThread;
    public Action<List<LobbyInfo>> OnLobbiesUpdated;
    public List<LobbyInfo> DiscoveredLobbies = new List<LobbyInfo>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        StartCoroutine(StartDiscovery());
    }

    IEnumerator StartDiscovery()
    {
        yield return new WaitForSeconds(1f);
        try
        {
            udpClient = new UdpClient(port);
            udpClient.EnableBroadcast = true;
            broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, port);
            listenThread = new Thread(ListenForBroadcasts);
            listenThread.Start();
            StartCoroutine(SendDiscoveryRequest());
        }
        catch (SocketException e)
        {
            Debug.LogError($"SocketException: {e.Message}. Port {port} may be in use.");
        }
    }

    void ListenForBroadcasts()
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, port);
        while (true)
        {
            try
            {
                byte[] data = udpClient.Receive(ref remoteEndPoint);
                string message = Encoding.UTF8.GetString(data);
                if (message.StartsWith("LOBBY:"))
                {
                    var info = JsonUtility.FromJson<LobbyInfo>(message.Substring(6));
                    info.ip = remoteEndPoint.Address.ToString();
                    info.port = 7777;
                    UpdateLobbyList(info);
                }
            }
            catch { }
        }
    }

    void UpdateLobbyList(LobbyInfo newLobby)
    {
        DiscoveredLobbies.RemoveAll(l => l.ip == newLobby.ip);
        if (newLobby.isOpen || string.IsNullOrEmpty(newLobby.password))
            DiscoveredLobbies.Add(newLobby);
        OnLobbiesUpdated?.Invoke(new List<LobbyInfo>(DiscoveredLobbies));
    }

    IEnumerator SendDiscoveryRequest()
    {
        while (true)
        {
            if (udpClient != null)
            {
                udpClient.Send(Encoding.UTF8.GetBytes("DISCOVER"), 8, broadcastEndPoint);
            }
            yield return new WaitForSeconds(3f);
        }
    }

    public void BroadcastLobby(LobbyInfo info)
    {
        if (udpClient == null) return;
        string message = "LOBBY:" + JsonUtility.ToJson(info);
        byte[] data = Encoding.UTF8.GetBytes(message);
        udpClient.Send(data, data.Length, broadcastEndPoint);
    }

    void OnDestroy()
    {
        udpClient?.Close();
        listenThread?.Abort();
        if (Instance == this) Instance = null;
    }

    public List<LobbyInfo> GetDiscoveredLobbies()
    {
        return DiscoveredLobbies;
    }
}