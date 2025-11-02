using UnityEngine;
using Unity.Entities;

public class HUDBridge : MonoBehaviour
{
    private EntityQuery _query;

    void Start()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        // Создаем query для singleton HUDState
        _query = world.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<HUDState>());
    }

    void Update()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var em = world.EntityManager;
        if (_query.IsEmpty) return;

        // Берем singleton entity
        var entityArray = _query.ToEntityArray(Unity.Collections.Allocator.Temp);
        var hudEntity = entityArray[0];
        var hud = em.GetComponentData<HUDState>(hudEntity);
        entityArray.Dispose();

        // TODO: обновить UI Toolkit с hud.HP, hud.MaxHP, hud.XP, hud.Level
    }
}

public struct HUDState : IComponentData
{
    public int HP;
    public int MaxHP;
    public int Level;
    public int XP;
}
