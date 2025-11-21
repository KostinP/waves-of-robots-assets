using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SceneLoaderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var em = state.EntityManager;

        // Правильный поиск RPC — только по компоненту
        var q = em.CreateEntityQuery(ComponentType.ReadOnly<StartGameRPC>());
        var entities = q.ToEntityArray(Allocator.Temp);

        if (entities.Length == 0)
        {
            entities.Dispose();
            return;
        }

        if (entities.Length > 1)
        {
            Debug.LogWarning($"Found {entities.Length} StartGameRPC entities, removing duplicates…");
            for (int i = 1; i < entities.Length; i++)
                em.DestroyEntity(entities[i]);
        }

        // Загружаем сцены
        if (!SceneManager.GetSceneByName("ArenaSubscene").isLoaded)
            SceneManager.LoadSceneAsync("ArenaSubscene", LoadSceneMode.Additive);

        if (!SceneManager.GetSceneByName("EntitiesSubscene").isLoaded)
            SceneManager.LoadSceneAsync("EntitiesSubscene", LoadSceneMode.Additive);

        if (!SceneManager.GetSceneByName("SystemsSubscene").isLoaded)
            SceneManager.LoadSceneAsync("SystemsSubscene", LoadSceneMode.Additive);

        // Удаляем RPC
        em.DestroyEntity(entities[0]);
        entities.Dispose();

        Debug.Log("Game scenes loading started.");
    }
}
