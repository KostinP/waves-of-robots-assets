using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public struct JoinLobbyRPC : IRpcCommand
{
    public FixedString64Bytes PlayerName;
    public FixedString64Bytes Weapon;
    public FixedString64Bytes Password;
}
