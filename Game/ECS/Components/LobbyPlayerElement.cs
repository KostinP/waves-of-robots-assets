using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[InternalBufferCapacity(9)]
public struct LobbyPlayerElement : IBufferElementData   
{
    [GhostField] public FixedString64Bytes PlayerName;
    [GhostField] public FixedString64Bytes Weapon;
    [GhostField] public int ConnectionId;
    [GhostField] public int Ping;
}
