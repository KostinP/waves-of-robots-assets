using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct UIEventsBootstrapSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // Создаём сущность "UI Events"
        var e = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponent<UIEventsSingleton>(e);
        state.EntityManager.AddBuffer<UIEvent_LobbyUpdated>(e);

        // Система не нужна дальше — отключаем
        state.Enabled = false;
    }

    public void OnUpdate(ref SystemState state) { }
}
