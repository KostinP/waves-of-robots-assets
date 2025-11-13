using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

// Server-side system that reacts to new/disconnected connections
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ConnectionSystem : ISystem
{
    ComponentLookup<NetworkId> _netIdLookup;

    public void OnCreate(ref SystemState state)
    {
        _netIdLookup = state.GetComponentLookup<NetworkId>(true);
        state.RequireForUpdate<NetworkStreamConnection>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        // New connections: entities that have NetworkStreamConnection but not yet InGame
        using var connectedEntities = em.CreateEntityQuery(
            ComponentType.ReadOnly<NetworkId>(),
            ComponentType.ReadOnly<NetworkStreamConnection>(),
            ComponentType.Exclude<NetworkStreamInGame>()
        ).ToEntityArray(Allocator.Temp);

        foreach (var entity in connectedEntities)
        {
            var netId = em.GetComponentData<NetworkId>(entity).Value;
            var cmd = em.CreateEntity();
            em.AddComponentData(cmd, new SpawnPlayerCommand { ConnectionId = (ulong)netId });
            em.AddComponent<NetworkStreamInGame>(entity); // mark as in game

            UIManager.Instance?.OnPlayerJoined(netId, "New Player");
        }

        // Disconnected connections
        using var disconnectedEntities = em.CreateEntityQuery(
            ComponentType.ReadOnly<NetworkId>(),
            ComponentType.Exclude<NetworkStreamConnection>()
        ).ToEntityArray(Allocator.Temp);

        foreach (var entity in disconnectedEntities)
        {
            if (em.HasComponent<NetworkId>(entity))
            {
                int netId = em.GetComponentData<NetworkId>(entity).Value;
                UIManager.Instance?.OnPlayerLeft(netId);
            }
        }
    }
}
