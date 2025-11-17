using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerPrefabComponent>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var prefabQuery = SystemAPI.QueryBuilder().WithAll<PlayerPrefabComponent>().Build();

        Entity prefabEntity = Entity.Null;
        if (!prefabQuery.IsEmpty)
        {
            var owner = prefabQuery.GetSingletonEntity();
            prefabEntity = em.GetComponentData<PlayerPrefabComponent>(owner).Prefab;
        }

        if (prefabEntity == Entity.Null)
        {
            ecb.Playback(em);
            return;
        }

        // Проходим по всем командами спауна
        foreach (var (cmd, entity) in SystemAPI.Query<RefRO<SpawnPlayerCommand>>().WithEntityAccess())
        {
            var spawn = cmd.ValueRO;
            var playerEnt = ecb.Instantiate(prefabEntity);
            ecb.SetComponent(playerEnt, LocalTransform.FromPosition(spawn.Position));
            ecb.AddComponent(playerEnt, new PlayerComponent
            {
                Name = new FixedString64Bytes($"Player_{spawn.ConnectionId}"),
                ConnectionId = spawn.ConnectionId,
                Ping = 0
            });
            ecb.AddComponent(playerEnt, new GhostOwner { NetworkId = (int)spawn.ConnectionId }); // best-effort
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(em);
    }
}
