using UnityEngine;

// Простейший стор сохранения прогресса
public static class MetaProgressionManager
{
    private const string KEY_GOLD = "WOR_GOLD";

    public static int Gold
    {
        get => PlayerPrefs.GetInt(KEY_GOLD, 0);
        set => PlayerPrefs.SetInt(KEY_GOLD, value);
    }

    public static void AddGold(int amount)
    {
        Gold = Gold + amount;
        PlayerPrefs.Save();
    }
}