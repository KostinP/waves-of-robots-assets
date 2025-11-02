using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct BulletSystemNet : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        float dt = SystemAPI.Time.DeltaTime;

        foreach (var (local, bullet, entity) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<BulletNet>>().WithEntityAccess().WithAll<Simulate>())
        {
            local.ValueRW.Position += new float3(0, 0, 1) * bullet.ValueRW.Speed * dt; // TODO: direction
            if (state.World.IsServer())
            {
                bullet.ValueRW.TimeToLive -= dt;
                if (bullet.ValueRW.TimeToLive <= 0f)
                {
                    ecb.DestroyEntity(entity);
                }
            }
        }

        ecb.Playback(state.EntityManager);
    }
}