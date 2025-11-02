using UnityEngine;

[CreateAssetMenu(menuName = "WOR/UpgradeLibrary")]
public class UpgradeLibrary : ScriptableObject
{
    public UpgradeData[] allUpgrades;

    public UpgradeData[] GetRandomChoices(int count)
    {
        // простая реализация
        var list = new System.Collections.Generic.List<UpgradeData>(allUpgrades);
        var res = new UpgradeData[count];
        for (int i = 0; i < count; i++)
        {
            if (list.Count == 0) break;
            int idx = Random.Range(0, list.Count);
            res[i] = list[idx];
            list.RemoveAt(idx);
        }
        return res;
    }
}