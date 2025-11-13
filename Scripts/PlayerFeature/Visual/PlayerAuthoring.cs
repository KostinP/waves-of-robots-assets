using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;

public class PlayerAuthoring : MonoBehaviour
{
    // Поставь этот скрипт на prefab игрока
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Добавляем компоненты ИГРОКА
        AddComponent(entity, LocalTransform.FromPosition(Vector3.zero));
        AddComponent(entity, new PlayerComponent
        {
            Name = new Unity.Collections.FixedString128Bytes("Player"),
            ConnectionId = 0,
            Ping = 0
        });

        // ❌ НЕ ДОБАВЛЯЙ PlayerPrefabComponent на игрока!
        // AddComponent<PlayerPrefabComponent>(entity); // УДАЛИ ЭТУ СТРОКУ
    }
}
