using Unity.NetCode;
using Unity.Entities;
using Unity.Mathematics;

public struct PlayerInputCommand : IInputComponentData
{
    public float2 MoveDir;
}
