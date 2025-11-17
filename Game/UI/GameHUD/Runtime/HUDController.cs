using UnityEngine;
using UnityEngine.UIElements;

using UiLabel = UnityEngine.UIElements.Label;

public class HUDController : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _players;

    private void Start()
    {
        var doc = GetComponent<UIDocument>();
        _root = doc.rootVisualElement;

        // В UXML у тебя это элемент с классом, НЕ name
        // Поэтому ищем по классу
        _players = _root.Q<VisualElement>(className: "hud__players");

        InvokeRepeating(nameof(UpdateHUD), 0f, 1f);
    }

    private void UpdateHUD()
    {
        if (HUDDataBridge.Instance == null)
            return;

        var list = HUDDataBridge.Instance.Players;

        _players.Clear();

        foreach (var (name, ping) in list)
        {
            var row = new VisualElement();
            row.AddToClassList("hud__player");

            row.Add(new UiLabel(name));
            row.Add(new UiLabel($"{ping} ms"));

            _players.Add(row);
        }
    }
}
