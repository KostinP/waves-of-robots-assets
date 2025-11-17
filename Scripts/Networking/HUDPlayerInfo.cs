using Unity.Entities;
using Unity.Collections;

public struct HUDPlayerInfo : IBufferElementData
{
    public FixedString32Bytes Name;
    public int Ping;
}
