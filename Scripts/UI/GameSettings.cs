using UnityEngine;
using System;

[Serializable]
public class GameSettings
{
    // Аудио
    public float musicVolume = 0.8f;
    public float soundVolume = 0.8f;

    // Графика
    public int resolutionIndex = 0;
    public int qualityLevel = 2;
    public bool fullscreen = true;

    // Геймплей
    public string playerName = "Player";
    public int defaultWeaponIndex = 0;
    public string selectedCharacter = "Toaster";

    // Язык
    public SystemLanguage language = SystemLanguage.Russian;
}