using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SpawnSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var prefabEntity = SystemAPI.QueryBuilder()
            .WithAll<PlayerPrefabComponent>()
            .Build()
            .TryGetSingleton(out PlayerPrefabComponent prefabData)
            ? prefabData.Prefab
            : Entity.Null;

        foreach (var (spawnCmd, entity) in SystemAPI.Query<RefRO<SpawnPlayerCommand>>().WithEntityAccess())
        {
            if (prefabEntity == Entity.Null)
                continue;

            var playerEntity = ecb.Instantiate(prefabEntity);
            ecb.SetComponent(playerEntity, LocalTransform.FromPosition(float3.zero));
            ecb.AddComponent(playerEntity, new PlayerComponent
            {
                Name = new FixedString128Bytes("Player"),
                ConnectionId = spawnCmd.ValueRO.ConnectionId,
                Ping = 0
            });
            ecb.AddComponent(playerEntity, new GhostOwner());
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}
