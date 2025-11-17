using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct LobbyClientUISystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingletonEntity<LobbyDataComponent>(out var lobbyEntity))
            return;

        var em = state.EntityManager;
        var buffer = em.GetBuffer<LobbyPlayerElement>(lobbyEntity);

        var evtEntity = SystemAPI.GetSingletonEntity<UIEventsSingleton>();
        var queue = SystemAPI.GetBuffer<UIEvent_LobbyUpdated>(evtEntity);

        queue.Clear();

        foreach (var p in buffer)
        {
            queue.Add(new UIEvent_LobbyUpdated
            {
                Name = p.PlayerName,
                Weapon = p.Weapon,
                Ping = p.Ping,
                ConnectionId = p.ConnectionId,
                IsLocalPlayer = false, // добавишь логику потом
                IsHost = false
            });
        }
    }
}
