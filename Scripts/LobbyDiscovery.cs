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

    private Dictionary<string, float> _lobbyLastSeen = new Dictionary<string, float>();
    private float _cleanupInterval = 5f; // проверяем каждые 5 секунд
    private float _lobbyTimeout = 10f; // лобби считается устаревшим после 10 секунд

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

        // УВЕЛИЧЬТЕ ТАЙМАУТ ДЛЯ ТЕСТИРОВАНИЯ
        _lobbyTimeout = 30f; // 30 секунд вместо 10
        _cleanupInterval = 10f; // Проверяем каждые 10 секунд вместо 5
    }

    //private IEnumerator CleanupExpiredLobbies()
    //{
    //    while (true)
    //    {
    //        yield return new WaitForSeconds(_cleanupInterval);

    //        lock (DiscoveredLobbies)
    //        {
    //            Debug.Log($"CleanupExpiredLobbies: Starting cleanup, current lobbies: {DiscoveredLobbies.Count}");

    //            float currentTime = Time.time;
    //            var lobbiesToRemove = new List<string>();

    //            foreach (var lobbyId in _lobbyLastSeen.Keys)
    //            {
    //                float lastSeen = _lobbyLastSeen[lobbyId];
    //                float timeSinceLastSeen = currentTime - lastSeen;
    //                Debug.Log($"CleanupExpiredLobbies: Lobby {lobbyId} - last seen: {lastSeen}, time since: {timeSinceLastSeen}, timeout: {_lobbyTimeout}");

    //                if (timeSinceLastSeen > _lobbyTimeout)
    //                {
    //                    lobbiesToRemove.Add(lobbyId);
    //                    Debug.Log($"CleanupExpiredLobbies: Marking lobby {lobbyId} for removal (timeout)");
    //                }
    //            }

    //            foreach (var lobbyId in lobbiesToRemove)
    //            {
    //                DiscoveredLobbies.RemoveAll(l => l.uniqueId == lobbyId);
    //                _lobbyLastSeen.Remove(lobbyId);
    //                Debug.Log($"CleanupExpiredLobbies: Removed expired lobby: {lobbyId}");
    //            }

    //            if (lobbiesToRemove.Count > 0)
    //            {
    //                _needsLobbyUpdate = true;
    //                Debug.Log($"CleanupExpiredLobbies: Removed {lobbiesToRemove.Count} lobbies, scheduling UI update");
    //            }


    //            Debug.Log($"CleanupExpiredLobbies: Cleanup completed, lobbies remaining: {DiscoveredLobbies.Count}");
    //        }
    //    }
    //}

    private IEnumerator CleanupExpiredLobbies()
    {
        while (true)
        {
            yield return new WaitForSeconds(30f); // Увеличьте интервал до 30 секунд
            Debug.Log("CleanupExpiredLobbies: Skipped for debugging");
        }
    }


    private void Start() => StartCoroutine(StartDiscovery());

    private void Update()
    {
        if (_needsLobbyUpdate)
        {
            _needsLobbyUpdate = false;
            Debug.Log($"LobbyDiscovery: Updating UI with {DiscoveredLobbies.Count} lobbies");
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

            Debug.Log($"LobbyDiscovery: Bound to port {broadcastPort}, broadcasting to {broadcastEndPoint}");

            listenThread = new Thread(ListenForBroadcasts) { IsBackground = true };
            listenThread.Start();
            StartCoroutine(SendDiscoveryRequest());
            StartCoroutine(CleanupExpiredLobbies());

            isInitialized = true;
            Debug.Log($"LobbyDiscovery initialized (port {broadcastPort}, ID={uniqueId})");
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyDiscovery init failed: {e.Message}");
        }
    }

    public void ForceDiscovery()
    {
        DebugNetworkInfo();

        if (!isHost && udpClient != null)
        {
            try
            {
                udpClient.Send(Encoding.UTF8.GetBytes("DISCOVER"), 7, broadcastEndPoint);
                Debug.Log("Forced DISCOVER request sent");
            }
            catch (Exception e)
            {
                Debug.LogError($"Forced discovery failed: {e.Message}");
            }
        }
    }

    private void ListenForBroadcasts()
    {
        var remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
        Debug.Log("LobbyDiscovery: Starting to listen for broadcasts...");

        while (true)
        {
            try
            {
                var data = udpClient.Receive(ref remoteEndPoint);
                var msg = Encoding.UTF8.GetString(data);
                Debug.Log($"Received raw message from {remoteEndPoint}: {msg}");

                if (msg.StartsWith("LOBBY:"))
                {
                    Debug.Log("Processing LOBBY message...");
                    var json = msg.Substring(6);
                    Debug.Log($"LOBBY JSON: {json}");

                    var info = JsonUtility.FromJson<LobbyInfo>(json);
                    Debug.Log($"Parsed lobby: {info.name}, uniqueId: {info.uniqueId}, myId: {uniqueId}");

                    if (info.uniqueId == uniqueId)
                    {
                        Debug.Log($"Ignoring own lobby: {info.uniqueId}");
                        continue;
                    }

                    info.ip = remoteEndPoint.Address.ToString();
                    if (info.port == 0) info.port = gamePort;

                    Debug.Log($"Discovered lobby: {info.name} from {info.ip}:{info.port}");

                    // КРИТИЧЕСКИЙ ФИКС: ВЫЗЫВАЕМ UpdateLobbyList
                    UpdateLobbyList(info);
                    _needsLobbyUpdate = true;
                    Debug.Log($"LobbyDiscovery: UpdateLobbyList called and UI update scheduled for {info.name}");
                }
                else if (msg.StartsWith("LOBBY_CLOSE:"))
                {
                    string closedLobbyId = msg.Substring(12);
                    Debug.Log($"Received lobby close: {closedLobbyId}");
                    RemoveLobbyById(closedLobbyId);
                }
                else if (msg == "DISCOVER" && isHost)
                {
                    Debug.Log("Received DISCOVER request, broadcasting lobby");
                    BroadcastLobby(currentLobbyInfo);
                }
                else
                {
                    Debug.Log($"Unknown message type: {msg}");
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

    private void RemoveLobbyById(string lobbyId)
    {
        bool wasRemoved = false;
        lock (DiscoveredLobbies)
        {
            int removed = DiscoveredLobbies.RemoveAll(l => l.uniqueId == lobbyId);
            if (removed > 0)
            {
                _lobbyLastSeen.Remove(lobbyId); // УДАЛЯЕМ ИЗ ТРЕКЕРА ВРЕМЕНИ
                wasRemoved = true;
                Debug.Log($"Removed lobby from discovery: {lobbyId}");
            }
        }

        // ВЫЗЫВАЕМ ОБНОВЛЕНИЕ UI ЕСЛИ ЛОББИ БЫЛО УДАЛЕНО
        if (wasRemoved)
        {
            _needsLobbyUpdate = true;
            Debug.Log($"Lobby {lobbyId} removed, scheduling UI update");
        }
    }

    private void UpdateLobbyList(LobbyInfo newLobby)
    {
        Debug.Log($"=== UpdateLobbyList START ===");

        try
        {
            lock (DiscoveredLobbies)
            {
                Debug.Log($"Adding lobby: {newLobby.name} (ID: {newLobby.uniqueId})");

                // Простая логика - всегда добавляем
                int existingIndex = DiscoveredLobbies.FindIndex(l => l.uniqueId == newLobby.uniqueId);
                if (existingIndex >= 0)
                {
                    DiscoveredLobbies[existingIndex] = newLobby;
                    Debug.Log($"Updated existing lobby at index {existingIndex}");
                }
                else
                {
                    DiscoveredLobbies.Add(newLobby);
                    Debug.Log($"Added new lobby. Total count: {DiscoveredLobbies.Count}");
                }

                // Обновляем время последнего видения
                _lobbyLastSeen[newLobby.uniqueId] = Time.time;

                // Выводим все лобби для отладки
                Debug.Log($"Current lobbies in list ({DiscoveredLobbies.Count}):");
                foreach (var lobby in DiscoveredLobbies)
                {
                    Debug.Log($"  - {lobby.name} (ID: {lobby.uniqueId}, IP: {lobby.ip}:{lobby.port})");
                }
            }

            _needsLobbyUpdate = true;
            Debug.Log($"UI update scheduled");
        }
        catch (Exception e)
        {
            Debug.LogError($"UpdateLobbyList ERROR: {e.Message}\n{e.StackTrace}");
        }

        Debug.Log($"=== UpdateLobbyList END ===");
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
                    Debug.Log($"Sent DISCOVER request to {broadcastEndPoint}");
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to send DISCOVER: {e.Message}");
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
            Debug.Log($"Broadcasted lobby to {broadcastEndPoint}: {info.name} at {info.ip}:{info.port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Broadcast failed: {e.Message}");
        }
    }

    public void StopHostingAndNotify()
    {
        if (!isHost) return;

        // ОТПРАВЛЯЕМ СООБЩЕНИЕ О ЗАКРЫТИИ ЛОББИ
        try
        {
            string closeMessage = "LOBBY_CLOSE:" + uniqueId;
            byte[] data = Encoding.UTF8.GetBytes(closeMessage);
            udpClient.Send(data, data.Length, broadcastEndPoint);
            Debug.Log($"Sent lobby close notification: {uniqueId}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to send lobby close notification: {e.Message}");
        }

        // ОСТАНАВЛИВАЕМ ХОСТИНГ
        isHost = false;
    }

    public void DebugNetworkInfo()
    {
        Debug.Log($"LobbyDiscovery Debug Info:");
        Debug.Log($"- Initialized: {isInitialized}");
        Debug.Log($"- Is Host: {isHost}");
        Debug.Log($"- My ID: {uniqueId}");
        Debug.Log($"- Discovered Lobbies: {DiscoveredLobbies.Count}");
        Debug.Log($"- Broadcast Port: {broadcastPort}");
        Debug.Log($"- Game Port: {gamePort}");

        foreach (var lobby in DiscoveredLobbies)
        {
            Debug.Log($"  - Lobby: {lobby.name} ({lobby.ip}:{lobby.port})");
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
        List<LobbyInfo> result;
        lock (DiscoveredLobbies)
        {
            result = new List<LobbyInfo>(DiscoveredLobbies);
            Debug.Log($"=== GetDiscoveredLobbies ===");
            Debug.Log($"Returning {result.Count} lobbies");

            foreach (var lobby in result)
            {
                Debug.Log($"  - Returning: {lobby.name} (ID: {lobby.uniqueId})");
            }
            Debug.Log($"=== End GetDiscoveredLobbies ===");
        }
        return result;
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