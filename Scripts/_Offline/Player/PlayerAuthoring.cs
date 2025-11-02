using Unity.Entities;
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour
{
    public int maxHP = 100;
    public float moveSpeed = 6f;

    public class Baker : Baker<PlayerAuthoring>
    {
        public override void Bake(PlayerAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new PlayerTag { });
            AddComponent(e, new PlayerStats { HP = authoring.maxHP, MaxHP = authoring.maxHP, MoveSpeed = authoring.moveSpeed });
            // HUDState обновляется системами
        }
    }
}

public struct PlayerTag : IComponentData { }

public struct PlayerStats : IComponentData
{
    public int HP;
    public int MaxHP;
    public float MoveSpeed;
}
