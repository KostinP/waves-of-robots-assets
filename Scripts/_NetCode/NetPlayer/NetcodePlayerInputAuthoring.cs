using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;

public class NetcodePlayerInputAuthoring : UnityEngine.MonoBehaviour
{
    public class Baker : Baker<NetcodePlayerInputAuthoring>
    {
        public override void Bake(NetcodePlayerInputAuthoring authoring)
        {
            var e = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(e, new NetcodePlayerInput());
        }
    }
}

// ✅ Структура должна быть value type
public struct NetcodePlayerInput : IInputComponentData
{
    public float2 inputVector;
    public bool shoot; // простая замена InputEvent
}
