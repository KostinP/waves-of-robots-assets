using Unity.Entities;
using Unity.Collections;

public struct UIEvent_LobbyUpdated : IBufferElementData
{
    public FixedString64Bytes Name;
    public FixedString64Bytes Weapon;
    public int Ping;
    public int ConnectionId;
    public bool IsLocalPlayer;
    public bool IsHost;
}
