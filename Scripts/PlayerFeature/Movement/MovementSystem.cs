using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using System;
using System.Numerics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MovementSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<PlayerInputCommand>();
    }

    public void OnUpdate(ref SystemState state)
    {
        float dt = SystemAPI.Time.DeltaTime;
        float speed = 5f;

        foreach (var (trans, input) in
            SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInputCommand>>())
        {
            float2 dir = input.ValueRO.MoveDir;
            if (math.lengthsq(dir) > 0f)
            {
                float3 forward = new float3(dir.x, 0f, dir.y);
                trans.ValueRW.Position += forward * speed * dt;

                float3 fwd = math.normalize(new float3(dir.x, 0f, dir.y));
                trans.ValueRW.Rotation = quaternion.LookRotationSafe(fwd, math.up());
            }
        }
    }
}
