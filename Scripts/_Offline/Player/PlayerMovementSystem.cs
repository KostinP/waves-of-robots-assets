using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        foreach (var (input, stats, local) in SystemAPI.Query<RefRO<PlayerInput>, RefRO<PlayerStats>, RefRW<LocalTransform>>().WithAll<PlayerTag>())
        {
            var dir = new float3(input.ValueRO.Move.x, 0, input.ValueRO.Move.y);
            if (math.lengthsq(dir) > 0.0001f)
            {
                dir = math.normalize(dir);
                local.ValueRW.Position += dir * stats.ValueRO.MoveSpeed * dt;
                // Optional: rotate towards movement
                local.ValueRW.Rotation = quaternion.LookRotationSafe(dir, math.up());
            }
        }
    }
}