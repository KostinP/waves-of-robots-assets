using Unity.Entities;
using Unity.Collections;

public struct PlayerComponent : IComponentData
{
    // Для NetCode / Ghost — пометь поля [GhostField] если будешь использовать ghost serialization
    // Тут — простые поля в компоненте
    public FixedString64Bytes Name;
    public ulong ConnectionId;
    public int Ping;
}
