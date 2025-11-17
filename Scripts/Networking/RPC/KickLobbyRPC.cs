using Unity.Entities;
using Unity.NetCode;

public struct KickLobbyRPC : IRpcCommand
{
    public int TargetConnectionId;
}
