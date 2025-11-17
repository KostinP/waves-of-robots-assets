using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct LobbyServerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<LobbyDataComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var lobbyEntity = SystemAPI.GetSingletonEntity<LobbyDataComponent>();
        var buffer = em.GetBuffer<LobbyPlayerElement>(lobbyEntity);

        // Удаляем игроков без connection
        var aliveConnections = new NativeHashSet<int>(buffer.Length, Allocator.Temp);

        foreach (var (id, stream) in
            SystemAPI.Query<RefRO<NetworkId>, RefRO<NetworkStreamInGame>>())
        {
            aliveConnections.Add(id.ValueRO.Value);
        }

        for (int i = buffer.Length - 1; i >= 0; i--)
        {
            if (!aliveConnections.Contains(buffer[i].ConnectionId))
                buffer.RemoveAt(i);
        }
    }
}
