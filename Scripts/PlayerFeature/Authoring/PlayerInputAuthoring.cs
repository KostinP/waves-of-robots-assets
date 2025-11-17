using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

public class PlayerInputAuthoring : MonoBehaviour { }

public class PlayerInputBaker : Baker<PlayerInputAuthoring>
{
    public override void Bake(PlayerInputAuthoring authoring)
    {
        var entity = GetEntity(TransformUsageFlags.Dynamic);
        AddComponent<PlayerInputCommand>(entity);
        AddComponent(entity, new PlayerInputCommand { MoveDir = float2.zero });
    }
}
