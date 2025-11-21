using Unity.Entities;
using Unity.Collections;

public struct PlayerComponent : IComponentData
{
    public FixedString64Bytes Name;
    public int Ping;
}