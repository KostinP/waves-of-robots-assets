// Assets/Scripts/InputSystem.cs
using Unity.Entities;
using UnityEngine.InputSystem;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct InputSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            var mousePos = Mouse.current.position.ReadValue();
            foreach (var input in SystemAPI.Query<RefRW<PlayerInputComponent>>())
            {
                input.ValueRW.IsClicked = true;
                input.ValueRW.MouseClickPosition = new float2(mousePos.x, mousePos.y);
            }
        }
    }
}