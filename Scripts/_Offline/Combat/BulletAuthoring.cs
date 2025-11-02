using Unity.Entities;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public float lifetime = 5f;
    public float speed = 10f;
    public int damage = 10;

    public class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new Bullet { TimeToLive = a.lifetime, Speed = a.speed, Damage = a.damage });
        }
    }
}

public struct Bullet : IComponentData
{
    public float TimeToLive;
    public float Speed;
    public int Damage;
}