using Unity.Entities;
using Unity.NetCode;

public struct LobbyStateResponseRPC : IRpcCommand
{
    public int Dummy; // Необходим для NetCode (RPC не может быть пустым)
}
