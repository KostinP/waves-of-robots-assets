using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

// Attach alongside LobbyDiscoveryService on the host (advertiseAsServer = true)
public class LobbyPreconnectServer : MonoBehaviour
{
    [Header("Preconnect")]
    public int queryPortOffset = 1; // queryPort = broadcastPort + offset
    public int playerVersion = 1;

    private UdpClient _udp;
    private IPEndPoint _localEp;
    private volatile bool _running;

    private LobbyDiscoveryService _discovery; // to read server settings (name, maxplayers, password)

    private void Awake()
    {
        _discovery = FindObjectOfType<LobbyDiscoveryService>();
        if (_discovery == null)
        {
            Debug.LogWarning("LobbyPreconnectServer: LobbyDiscoveryService not found — attach this only on host.");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        int port = Mathf.Max(1024, _discovery != null ? _discovery.broadcastPort + queryPortOffset : 7980);
        _localEp = new IPEndPoint(IPAddress.Any, port);
        try
        {
            _udp = new UdpClient(_localEp);
            _running = true;
            Task.Run(ReceiveLoopAsync);
            Debug.Log($"LobbyPreconnectServer started on port {port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"LobbyPreconnectServer failed to bind port {port}: {e}");
            _running = false;
        }
    }

    private async Task ReceiveLoopAsync()
    {
        while (_running)
        {
            try
            {
                var result = await _udp.ReceiveAsync();
                ProcessDatagram(result.Buffer, result.RemoteEndPoint);
            }
            catch (ObjectDisposedException) { break; }
            catch (Exception e)
            {
                Debug.LogWarning($"Preconnect receive error: {e}");
            }
        }
    }

    private void ProcessDatagram(byte[] data, IPEndPoint from)
    {
        try
        {
            var json = Encoding.UTF8.GetString(data);
            var query = JsonUtility.FromJson<PreconnectQuery>(json);
            if (query == null || query.type != "QueryJoin")
                return;

            // Basic checks: version, password, slot availability
            var resp = new PreconnectResponse();

            if (query.playerVersion != playerVersion)
            {
                resp.result = "VersionMismatch";
                resp.reason = "Client version mismatch";
            }
            else if (!_discovery.advertiseAsServer)
            {
                resp.result = "LobbyFull";
                resp.reason = "Server not accepting players";
            }
            else
            {
                // check password
                string expected = _discovery.serverPassword ?? "";
                bool isProtected = !string.IsNullOrEmpty(expected);

                if (isProtected && expected != (query.password ?? ""))
                {
                    resp.result = "BadPassword";
                    resp.reason = "Wrong password";
                }
                else
                {
                    // slot check — naive count (could be improved by querying netcode world)
                    // Here we just accept for now; if you want exact check, integrate with NetCode connection count.
                    resp.result = "OK";
                    resp.reason = "Welcome";
                }
            }

            var outBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(resp));
            _udp.Send(outBytes, outBytes.Length, from);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed process preconnect datagram: {e}");
        }
    }

    private void OnDestroy()
    {
        _running = false;
        try { _udp?.Close(); _udp?.Dispose(); } catch { }
    }
}
