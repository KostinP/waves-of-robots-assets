✅ GameManager.cs
- Singleton менеджер игры
- Создаёт другие менеджеры (LocalizationManager, UIManager, SettingsManager, LobbyManager)
- Загружает сцены MainMenu и Game
- Находится вне DOTS, чистый MonoBehaviour

✅ StartupManager.cs
- Загружает сцену MainMenu при старте проекта
- MonoBehaviour, сцена Bootstrap
- Никаких зависимостей от UI/NetCode/ECS