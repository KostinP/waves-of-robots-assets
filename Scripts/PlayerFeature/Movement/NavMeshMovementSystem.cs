// Assets/Scripts/NavMeshMovementSystem.cs
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine.AI;
using UnityEngine; // Для Camera и Physics

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct NavMeshMovementSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (transform, input) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<PlayerInputComponent>>())
        {
            if (input.ValueRO.IsClicked)
            {
                var ray = Camera.main.ScreenPointToRay(new Vector3(input.ValueRO.MouseClickPosition.x, input.ValueRO.MouseClickPosition.y, 0));
                if (Physics.Raycast(ray, out var hit))
                {
                    var path = new NavMeshPath();
                    NavMesh.CalculatePath(transform.ValueRO.Position, hit.point, NavMesh.AllAreas, path);
                    // Implement path following in a component, here simplified lerp
                    transform.ValueRW.Position = math.lerp(transform.ValueRO.Position, hit.point, 0.1f);
                }
            }
        }
    }
}