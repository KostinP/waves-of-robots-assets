using Unity.Entities;
using Unity.Networking.Transport;

public struct NetworkStreamRequestConnect : IComponentData
{
    public NetworkEndpoint Endpoint;
}