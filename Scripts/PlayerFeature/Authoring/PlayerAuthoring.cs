using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;

[DisallowMultipleComponent]
public class PlayerAuthoring : MonoBehaviour
{
    // Поставь этот скрипт на GameObject-префаб, который будет конвертироваться в ECS-префаб
    // Никакого кода здесь — Baker делает всё
}

public class PlayerBaker : Baker<PlayerAuthoring>
{
    public override void Bake(PlayerAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);

        // Добавляем минимальные компоненты префаба
        AddComponent(entity, LocalTransform.FromPosition(Unity.Mathematics.float3.zero));

        AddComponent(entity, new PlayerComponent
        {
            Name = new FixedString64Bytes("Player"),
            ConnectionId = 0,
            Ping = 0
        });

        // НЕ добавляем PlayerPrefabComponent сюда — это добавит Baker ниже
    }
}
