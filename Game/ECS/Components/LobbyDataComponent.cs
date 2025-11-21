using Unity.Entities;
using Unity.Collections;

public struct LobbyDataComponent : IComponentData
{
    public FixedString64Bytes Name;
    public int MaxPlayers;
    public FixedString64Bytes Password;
    public int CurrentPlayers;
    public byte IsStarted; // 0 = not started, 1 = started
}
