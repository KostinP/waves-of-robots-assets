using Unity.Collections;

public struct LobbyPlayerInfo
{
    public int ConnectionId;
    public FixedString64Bytes Name;
    public int Ping;
    public FixedString64Bytes Character;
    public FixedString64Bytes Weapon;
    public bool IsLocalPlayer;
    public bool IsHost;
}