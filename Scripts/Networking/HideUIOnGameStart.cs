using Unity.Entities;
using UnityEngine;

public struct GameStarted : IComponentData { }

public class HideUIOnGameStart : MonoBehaviour
{
    private void Update()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        var entityManager = world.EntityManager;
        var query = entityManager.CreateEntityQuery(typeof(GameStarted));

        if (!query.IsEmpty)
        {
            gameObject.SetActive(false);
        }
    }
}