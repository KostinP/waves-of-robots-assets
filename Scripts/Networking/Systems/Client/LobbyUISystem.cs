using Unity.Entities;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct LobbyUISystem : ISystem
{
    double lastUpdate;

    public void OnUpdate(ref SystemState state)
    {
        // частота обновления — как у тебя было
        if (state.WorldUnmanaged.Time.ElapsedTime - lastUpdate < 0.3)
            return;

        lastUpdate = state.WorldUnmanaged.Time.ElapsedTime;

        var em = state.EntityManager;

        if (!SystemAPI.TryGetSingletonEntity<LobbyDataComponent>(out var lobbyEntity))
            return;

        if (!em.HasBuffer<LobbyPlayerElement>(lobbyEntity))
            return;

        var buffer = em.GetBuffer<LobbyPlayerElement>(lobbyEntity);

        // UIEvents singleton должен существовать
        if (!SystemAPI.TryGetSingletonEntity<UIEventsSingleton>(out var uiEventsEntity))
            return;

        var uiBuffer = em.GetBuffer<UIEvent_LobbyUpdated>(uiEventsEntity);

        // Очистим старые события и заполним новыми записями
        uiBuffer.Clear();

        foreach (var p in buffer)
        {
            uiBuffer.Add(new UIEvent_LobbyUpdated
            {
                Name = p.PlayerName,
                Weapon = p.Weapon,
                Ping = p.Ping,
                ConnectionId = p.ConnectionId,
                IsLocalPlayer = false,
                IsHost = false
            });
        }
    }
}
