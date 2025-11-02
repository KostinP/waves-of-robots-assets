using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using UnityEngine.SceneManagement;

[UpdateInGroup(typeof(InitializationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct NetworkUISystem : ISystem
{
    private bool _uiSceneLoaded;
    private EntityQuery _networkIdQuery;

    public void OnCreate(ref SystemState state)
    {
        _networkIdQuery = state.GetEntityQuery(ComponentType.ReadOnly<NetworkId>());
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        // Проверяем что мы клиент
        if (!state.World.IsClient()) return;

        // Проверяем подключение через наличие NetworkId
        bool isConnected = !_networkIdQuery.IsEmpty;

        if (isConnected && !_uiSceneLoaded)
        {
            // Загружаем UI сцену аддитивно
            SceneManager.LoadSceneAsync("UIScene", LoadSceneMode.Additive);
            _uiSceneLoaded = true;
            Debug.Log("UI Scene loaded for connected client");
        }
        else if (!isConnected && _uiSceneLoaded)
        {
            // Выгружаем UI сцену при отключении
            SceneManager.UnloadSceneAsync("UIScene");
            _uiSceneLoaded = false;
            Debug.Log("UI Scene unloaded");
        }
    }

    public void OnDestroy(ref SystemState state)
    {
        // При разрушении системы выгружаем UI сцену
        if (_uiSceneLoaded)
        {
            SceneManager.UnloadSceneAsync("UIScene");
            _uiSceneLoaded = false;
        }
    }
}