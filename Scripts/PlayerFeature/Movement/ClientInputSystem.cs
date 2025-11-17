using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Unity.NetCode;
using System;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct ClientInputSystem : ISystem
{
    private Camera cam;

    public void OnCreate(ref SystemState state)
    {
        cam = Camera.main;
        state.RequireForUpdate<NetworkId>();
    }

    public void OnUpdate(ref SystemState state)
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        // позиция клика
        float2 dir = float2.zero;
        if (Input.GetMouseButton(0))
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out var hit))
            {
                float3 playerPos = float3.zero;

                // ищем локального игрока
                foreach (var (trans, owner) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<NetworkId>>())
                {
                    playerPos = trans.ValueRO.Position;
                    break;
                }

                float3 hitPos = hit.point;
                float3 rawDir = hitPos - playerPos;

                dir = math.normalize(new float2(rawDir.x, rawDir.z));
            }
        }

        // записываем в команду
        foreach (var input in SystemAPI.Query<RefRW<PlayerInputCommand>>().WithAll<GhostOwnerIsLocal>())
        {
            input.ValueRW.MoveDir = dir;
        }
    }
}
