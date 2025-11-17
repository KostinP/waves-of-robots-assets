using Unity.Entities;

public struct SpawnPlayerCommand : IComponentData
{
    public ulong ConnectionId;
    public Unity.Mathematics.float3 Position;
}
