using Unity.Entities;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial class LobbyAdvertiseSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireForUpdate<LobbyDataComponent>();
    }

    protected override void OnUpdate()
    {
        // Тут должен быть UDP broadcast    
    }
}
