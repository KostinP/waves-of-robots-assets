using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct PingGhostSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<NetworkSnapshotAck>())
            return;

        int ms = (int)(SystemAPI.GetSingleton<NetworkSnapshotAck>().EstimatedRTT * 1000f);

        var lobbyEntity = SystemAPI.GetSingletonEntity<LobbyDataComponent>();
        var buffer = SystemAPI.GetBuffer<LobbyPlayerElement>(lobbyEntity);

        var id = SystemAPI.GetSingleton<NetworkId>().Value;

        for (int i = 0; i < buffer.Length; i++)
            if (buffer[i].ConnectionId == id)
            {
                var elem = buffer[i];
                elem.Ping = ms;
                buffer[i] = elem;
            }
    }
}
