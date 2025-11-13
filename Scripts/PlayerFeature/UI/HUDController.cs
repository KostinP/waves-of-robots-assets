using UnityEngine;
using UnityEngine.UIElements;
using Unity.Entities;
using Unity.Collections;
using Unity.NetCode;
using System.Linq;

public class HUDController : MonoBehaviour
{
    private VisualElement _hudRoot;
    private VisualElement _playersContainer;

    void Start()
    {
        _hudRoot = GetComponent<UIDocument>().rootVisualElement;
        _playersContainer = _hudRoot.Q<VisualElement>("hud__players");
        InvokeRepeating(nameof(UpdatePlayerList), 0f, 1f);
    }

    public void UpdatePlayerList()
    {
        _playersContainer.Clear();
        var clientWorld = World.All.FirstOrDefault(w => w.IsClient());
        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<PlayerComponent>());
        var players = query.ToComponentDataArray<PlayerComponent>(Allocator.Temp);

        for (int i = 0; i < players.Length; i++)
        {
            var p = players[i];
            string name = p.Name.ToString();
            string ping = p.Ping.ToString() + "мс";
            CreatePlayerHUD(_playersContainer, name, ping);
        }
        players.Dispose();
    }

    private void CreatePlayerHUD(VisualElement container, string name, string ping)
    {
        var player = new VisualElement { pickingMode = PickingMode.Ignore };
        player.AddToClassList("hud__player");
        var icon = new VisualElement { pickingMode = PickingMode.Ignore };
        icon.AddToClassList("hud__player-icon");
        icon.AddToClassList("mic");
        var nameLabel = new Label(name) { pickingMode = PickingMode.Ignore };
        nameLabel.AddToClassList("hud__player-name");
        var pingLabel = new Label(ping) { pickingMode = PickingMode.Ignore };
        pingLabel.AddToClassList("hud__player-ping");
        float pingValue = float.Parse(ping.Replace("мс", ""));
        if (pingValue < 50) pingLabel.AddToClassList("good");
        else if (pingValue < 150) pingLabel.AddToClassList("medium");
        else pingLabel.AddToClassList("bad");
        player.Add(icon); player.Add(nameLabel); player.Add(pingLabel);
        container.Add(player);
    }
}