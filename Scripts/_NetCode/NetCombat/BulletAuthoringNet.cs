using Unity.Entities;
using UnityEngine;

public class BulletAuthoringNet : MonoBehaviour
{
    public float lifetime = 5f;
    public float speed = 12f;
    public int damage = 10;

    public class Baker : Baker<BulletAuthoringNet>
    {
        public override void Bake(BulletAuthoringNet a)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new BulletNet { TimeToLive = a.lifetime, Speed = a.speed, Damage = a.damage });
        }
    }
}

public struct BulletNet : IComponentData
{
    public float TimeToLive;
    public float Speed;
    public int Damage;
}