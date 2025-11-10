using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
using UnityEngine.LightTransport;

public class PlayerSpawnManager : MonoBehaviour
{
    private Entity prefabEntity;
    private EntityManager entityManager;

    void Start()
    {
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        // 1. Пытаемся найти из SubScene
        var query = entityManager.CreateEntityQuery(typeof(PlayerPrefabComponent));
        if (!query.IsEmptyIgnoreFilter)
        {
            var prefabComp = entityManager.GetComponentData<PlayerPrefabComponent>(query.GetSingletonEntity());
            prefabEntity = prefabComp.Prefab;
            Debug.Log("Player prefab loaded from SubScene.");
            return;
        }

        // 2. Если SubScene не загружена — создаём вручную
        Debug.Log("SubScene not loaded yet. Creating player prefab manually.");

        // Создаём префаб-сущность
        prefabEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(prefabEntity, LocalTransform.FromPosition(Vector3.zero));
        entityManager.AddComponentData(prefabEntity, new PlayerComponent
        {
            Name = new Unity.Collections.FixedString128Bytes("Player"),
            ConnectionId = 0,
            Ping = 0
        });

        // Опционально: создаём singleton для совместимости
        var singletonEntity = entityManager.CreateEntity();
        entityManager.AddComponentData(singletonEntity, new PlayerPrefabComponent { Prefab = prefabEntity });
    }

    public void SpawnPlayer(ulong connectionId, Vector3 spawnPoint)
    {
        if (prefabEntity == Entity.Null)
        {
            Debug.LogError("Player prefab not initialized!");
            return;
        }

        var playerEntity = entityManager.Instantiate(prefabEntity);
        entityManager.SetComponentData(playerEntity, LocalTransform.FromPosition(spawnPoint));
        entityManager.SetComponentData(playerEntity, new PlayerComponent
        {
            Name = new Unity.Collections.FixedString128Bytes("Player_" + connectionId),
            ConnectionId = connectionId,
            Ping = 0
        });

        Debug.Log($"Player spawned: ID={connectionId} at {spawnPoint}");
    }
}