using Unity.Entities;
using UnityEngine;

public class EnemyAuthoring : MonoBehaviour
{
    public int maxHp = 20;
    public float speed = 2f;

    public class Baker : Baker<EnemyAuthoring>
    {
        public override void Bake(EnemyAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new EnemyTag { });
            AddComponent(e, new Health { Current = authoring.maxHp, Max = authoring.maxHp });
            AddComponent(e, new EnemyStats { MoveSpeed = authoring.speed });
        }
    }
}

public struct EnemyTag : IComponentData { }
public struct EnemyStats : IComponentData { public float MoveSpeed; }