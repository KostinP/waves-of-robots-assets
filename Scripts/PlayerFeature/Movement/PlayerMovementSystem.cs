using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, input) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInputComponent>>())
        {
            if (input.ValueRO.IsClicked)
            {
                // movement placeholder
                transform.ValueRW.Position += new float3(0, 0, 1) * SystemAPI.Time.DeltaTime;
            }
        }
    }
}
