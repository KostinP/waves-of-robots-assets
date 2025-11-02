using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

// MonoBehaviour, который читает стандартный Input и пишет в компонент на первой сущности с PlayerTag
// Можно заменить на новую InputSystem при желании
public class PlayerInputBridge : MonoBehaviour
{
    // На сцене должен быть сущность-игрок (через SubScene)
    void Update()
    {
        var world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
        if (world == null) return;
        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(ComponentType.ReadWrite<PlayerInput>(), ComponentType.ReadOnly<PlayerTag>());
        using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
        {
            if (entities.Length == 0) return;
            var e = entities[0];
            var input = new PlayerInput();
            input.Move = new float2(UnityEngine.Input.GetAxisRaw("Horizontal"), UnityEngine.Input.GetAxisRaw("Vertical"));
            input.Shoot = UnityEngine.Input.GetButton("Fire1");
            entityManager.SetComponentData(e, input);
        }
    }
}