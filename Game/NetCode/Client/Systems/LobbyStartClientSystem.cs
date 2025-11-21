using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using System.Diagnostics;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
public partial struct LobbyStartClientSystem : ISystem
{
    double lastCheck;

    public void OnCreate(ref SystemState state)
    {
        // ничего
    }

    public void OnUpdate(ref SystemState state)
    {
        // Питер: проверяем раз в 0.2 сек
        if (state.WorldUnmanaged.Time.ElapsedTime - lastCheck < 0.2) return;
        lastCheck = state.WorldUnmanaged.Time.ElapsedTime;

        if (!SystemAPI.TryGetSingletonEntity<LobbyDataComponent>(out var lobbyEntity)) return;

        var ld = SystemAPI.GetComponent<LobbyDataComponent>(lobbyEntity);

        if (ld.IsStarted != 0)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Game");
        }
    }
}
