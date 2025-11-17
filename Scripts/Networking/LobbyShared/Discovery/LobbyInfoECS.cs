using Unity.Collections;

public struct LobbyInfoECS
{
    public FixedString64Bytes Name;
    public int PlayerCount;
    public int MaxPlayers;
    public bool IsOpen;
    public FixedString64Bytes Password;
}
