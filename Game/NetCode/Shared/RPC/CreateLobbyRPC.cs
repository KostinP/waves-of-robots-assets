using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public struct CreateLobbyRPC : IRpcCommand
{
    public FixedString64Bytes Name;
    public FixedString64Bytes Password;
    public int MaxPlayers;
    public FixedString64Bytes Character;
}
