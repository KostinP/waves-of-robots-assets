using Unity.Entities;
using UnityEngine;
using Unity.Transforms;

public class PlayerPrefabAuthoring : MonoBehaviour { }

public class PlayerPrefabBaker : Baker<PlayerPrefabAuthoring>
{
    public override void Bake(PlayerPrefabAuthoring authoring)
    {
        // Создаём сущность-префаб игрока
        var prefabEntity = CreateAdditionalEntity(TransformUsageFlags.None);

        // Копируем все компоненты с игрока
        AddComponent(prefabEntity, LocalTransform.FromPosition(Vector3.zero));
        AddComponent(prefabEntity, new PlayerComponent
        {
            Name = new Unity.Collections.FixedString128Bytes("Player"),
            ConnectionId = 0,
            Ping = 0
        });

        // Сохраняем префаб в singleton
        var holderEntity = GetEntity(TransformUsageFlags.None);
        AddComponent(holderEntity, new PlayerPrefabComponent { Prefab = prefabEntity });
    }
}