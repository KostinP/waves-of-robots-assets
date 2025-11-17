using Unity.Entities;
using Unity.Collections;

public struct HUDData : IComponentData
{
    public int Health;
    public FixedString64Bytes Name;
}
