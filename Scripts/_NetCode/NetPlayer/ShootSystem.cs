using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ShootSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var entitiesRefs = SystemAPI.GetSingleton<EntitiesReferences>();
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (input, local, owner) in SystemAPI.Query<RefRO<NetcodePlayerInput>, RefRO<LocalTransform>, RefRO<GhostOwner>>()
                     .WithAll<GhostOwnerIsLocal>())
        {
            if (!networkTime.IsFirstTimeFullyPredictingTick) continue;
            if (input.ValueRO.shoot)
            {
                var bullet = ecb.Instantiate(entitiesRefs.bulletPrefabEntity);
                ecb.SetComponent(bullet, LocalTransform.FromPosition(local.ValueRO.Position + new float3(0, 0, 1.5f)));
                ecb.AddComponent(bullet, new GhostOwner { NetworkId = owner.ValueRO.NetworkId });
            }
        }

        ecb.Playback(state.EntityManager);
    }
}
