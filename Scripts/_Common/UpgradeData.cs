using UnityEngine;

public enum UpgradeType { Weapon, Modifier, Passive }

[CreateAssetMenu(menuName = "WOR/UpgradeData")]
public class UpgradeData : ScriptableObject
{
    public string id;
    public string title;
    [TextArea] public string description;
    public Sprite icon;
    public UpgradeType type;
    public float value;
    // metadata: rarity, synergy tags, requires
}