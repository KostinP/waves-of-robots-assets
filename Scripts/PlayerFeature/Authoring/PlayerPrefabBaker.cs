using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

public class PlayerPrefabAuthoring : MonoBehaviour { }

public class PlayerPrefabBaker : Baker<PlayerPrefabAuthoring>
{
    public override void Bake(PlayerPrefabAuthoring authoring)
    {
        // Создаём дополнительную сущность-префаб (ADDITIONAL)
        var prefabEntity = CreateAdditionalEntity(TransformUsageFlags.None);

        AddComponent(prefabEntity, LocalTransform.FromPosition(Unity.Mathematics.float3.zero));
        AddComponent(prefabEntity, new PlayerComponent
        {
            Name = new Unity.Collections.FixedString64Bytes("Player"),
            ConnectionId = 0,
            Ping = 0
        });

        var holder = GetEntity(TransformUsageFlags.None);
        AddComponent(holder, new PlayerPrefabComponent { Prefab = prefabEntity });
    }
}
