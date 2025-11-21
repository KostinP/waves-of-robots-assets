using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct LobbyGhostInitSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkId>();

        // Создаём singleton, если отсутствует
        if (!SystemAPI.TryGetSingletonEntity<LobbyDataComponent>(out _))
        {
            var e = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(e, new LobbyDataComponent
            {
                MaxPlayers = 8,
                Password = ""
            });
            state.EntityManager.AddBuffer<LobbyPlayerElement>(e);
            state.EntityManager.AddComponent<GhostOwner>(e);
            state.EntityManager.AddComponent<GhostEnabled>(e);
        }
    }

    public void OnUpdate(ref SystemState state) { }
}
