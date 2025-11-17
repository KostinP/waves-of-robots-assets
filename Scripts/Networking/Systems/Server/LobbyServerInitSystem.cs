using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct LobbyServerInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        var q = state.EntityManager.CreateEntityQuery(typeof(LobbyDataComponent));
        if (!q.IsEmpty) return;

        var e = state.EntityManager.CreateEntity(typeof(LobbyDataComponent));
        state.EntityManager.AddBuffer<LobbyPlayerElement>(e);
    }
}
