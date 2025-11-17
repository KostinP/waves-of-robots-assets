using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using System.Collections;

public static class LobbyPreconnectClient
{
    // Starts a coroutine on the provided MonoBehaviour (runner).
    // Calls callback with PreconnectResult when complete.
    public static IEnumerator StartJoinQueryCoroutine(MonoBehaviour runner, string ip, int queryPort, string password, Action<PreconnectResult> callback, float timeout = 0.6f, int playerVersion = 1, string playerName = "Player")
    {
        if (runner == null) throw new ArgumentNullException(nameof(runner));
        if (string.IsNullOrEmpty(ip))
        {
            callback?.Invoke(PreconnectResult.NetworkError);
            yield break;
        }

        bool completed = false;
        PreconnectResult result = PreconnectResult.Unknown;

        // Run UDP send/receive on threadpool to avoid blocking main thread
        Task.Run(async () =>
        {
            using (var udp = new UdpClient())
            {
                try
                {
                    udp.Client.ReceiveTimeout = (int)(timeout * 1000);
                    var endpoint = new IPEndPoint(IPAddress.Parse(ip), queryPort);

                    var q = new PreconnectQuery
                    {
                        type = "QueryJoin",
                        password = password ?? "",
                        playerVersion = playerVersion,
                        playerName = playerName
                    };

                    var bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(q));
                    await udp.SendAsync(bytes, bytes.Length, endpoint);

                    var resp = await udp.ReceiveAsync(); // may throw on timeout
                    var json = Encoding.UTF8.GetString(resp.Buffer);
                    var r = JsonUtility.FromJson<PreconnectResponse>(json);

                    if (r == null)
                    {
                        result = PreconnectResult.Unknown;
                    }
                    else
                    {
                        switch (r.result)
                        {
                            case "OK": result = PreconnectResult.Ok; break;
                            case "BadPassword": result = PreconnectResult.BadPassword; break;
                            case "LobbyFull": result = PreconnectResult.LobbyFull; break;
                            case "VersionMismatch": result = PreconnectResult.VersionMismatch; break;
                            default: result = PreconnectResult.Unknown; break;
                        }
                    }
                }
                catch (SocketException)
                {
                    result = PreconnectResult.Timeout;
                }
                catch (Exception)
                {
                    result = PreconnectResult.NetworkError;
                }
            }

            completed = true;
        });

        float timer = 0f;
        float maxWait = timeout + 0.15f;

        while (!completed && timer < maxWait)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        // If the background task didn't complete, set timeout if unknown
        if (!completed && result == PreconnectResult.Unknown)
            result = PreconnectResult.Timeout;

        callback?.Invoke(result);
    }
}
