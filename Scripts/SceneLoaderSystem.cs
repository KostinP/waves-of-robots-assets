using Unity.Entities;
using Unity.Scenes;
using UnityEngine;
using UnityEngine.SceneManagement;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial struct SceneLoaderSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var query = SystemAPI.QueryBuilder().WithAll<StartGameCommand>().Build();
        var entities = query.ToEntityArray(Allocator.Temp);
        var count = entities.Length;

        if (count > 1)
        {
            Debug.LogWarning($"Found {count} StartGameCommand instances! Destroying extras.");
            for (int i = 1; i < count; i++)
            {
                state.EntityManager.DestroyEntity(entities[i]);
            }
        }

        if (count == 0)
        {
            entities.Dispose();
            return;
        }

        Debug.Log("SceneLoaderSystem: Loading game scenes...");

        // Загружаем сцены (Additive)
        SceneManager.LoadSceneAsync("ArenaSubscene", LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("EntitiesSubscene", LoadSceneMode.Additive);
        SceneManager.LoadSceneAsync("SystemsSubscene", LoadSceneMode.Additive);

        // Уничтожаем оставшуюся команду
        state.EntityManager.DestroyEntity(entities[0]);
        entities.Dispose();

        Debug.Log("Game scenes loading started");
    }
}