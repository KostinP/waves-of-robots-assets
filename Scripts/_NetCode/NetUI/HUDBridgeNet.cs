using UnityEngine;
using Unity.Entities;

public class HUDBridgeNet : MonoBehaviour
{
    private EntityQuery _query;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        // Query для singleton HUDState
        _query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HUDState>());
    }

    void Update()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var em = world.EntityManager;
        if (_query.IsEmpty) return;

        var entityArray = _query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var hudEntity = entityArray[0];
        var hud = em.GetComponentData<HUDState>(hudEntity);
        entityArray.Dispose();

        // TODO: обновить UI Toolkit с hud.HP, hud.MaxHP, hud.XP, hud.Level
    }
}
