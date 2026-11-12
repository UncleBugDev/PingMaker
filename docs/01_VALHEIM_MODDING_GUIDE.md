# Руководство по разработке модов для Valheim (BepInEx 5.4.x & HarmonyX)

Данный документ описывает официальные стандарты, проверенные паттерны и архитектурные правила создания профессиональных модов для игры Valheim без использования «костылей» и сомнительных хаков.

---

## 1. Базовая архитектура BepInEx 5.4

### 1.1 Точка входа плагина (`BaseUnityPlugin`)
Каждый мод для Valheim оформляется как класс, наследующий `BepInEx.BaseUnityPlugin`.

```csharp
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace PingMaker
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    public class PingMakerPlugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.author.pingmaker";
        public const string PluginName = "PingMaker";
        public const string PluginVersion = "1.0.0";

        public static PingMakerPlugin Instance { get; private set; }
        public static ManualLogSource Log => Instance.Logger;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;

            // 1. Инициализация конфигурации
            ModConfig.Init(Config);

            // 2. Инициализация патчей Harmony
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{PluginVersion} успешно инициализирован.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
```

### 1.2 Правила жизненного цикла (Lifecycle Rules)
1. **Никакой тяжелой логики в `Awake()`**: При вызове `Awake()` игровой мир (`Game.instance`), сетевой менеджер (`ZNet.instance`) и игрок (`Player.m_localPlayer`) ещё не существуют. В `Awake()` выполняется только чтение конфигурации, предварительная генерация процедурных ассетов и вызов Harmony.
2. **Безопасная отгрузка (`OnDestroy()`)**: Всегда вызывать `_harmony.UnpatchSelf()` и освобождать динамически созданные объекты/слушатели событий при выгрузке.
3. **Главный поток Unity**: Все обращения к компонентам Unity (`GameObject`, `Transform`, `Renderer`, `Material`) должны выполняться строго в главном потоке (Update, LateUpdate, Coroutines).

---

## 2. Стандарты патчинга через HarmonyX

### 2.1 Prefix vs Postfix vs Transpiler
* **`[HarmonyPrefix]`**:
  - Используется только когда нужно проверить условие перед выполнением оригинального метода.
  - Если не требуется прерывать ванильный метод, Prefix **всегда возвращает `true`** (`bool Prefix() => true;`).
  - **Запрещено** без крайней необходимости подавлять ванильный код (`return false;`), так как это ломает совместимость с другими модами.
* **`[HarmonyPostfix]`**:
  - Идеальное место для регистрации расширений (например, после завершения `Game.Start` или `ZNet.Awake`).
  - Позволяет безопасно реагировать на уже созданные ванильные синглтоны.
* **`[HarmonyTranspiler]`**:
  - Применяется только для точечной модификации IL-инструкций ванильных методов без переписывания всего тела метода.

### 2.2 Регистрация хуков для сетевых и игровых событий
В Valheim ключевые синглтоны появляются поэтапно:
1. `FejdStartup.Awake`: Главное меню.
2. `ZNet.Awake`: Инициализация сетевого стека (`ZRoutedRpc.instance`).
3. `Game.Start`: Загрузка мира, доступность `ZRoutedRpc`, `ZNetScene.instance`.
4. `Player.OnSpawned`: Появление персонажа игрока в мире.

---

## 3. Конфигурация через `BepInEx.Configuration`

Все пользовательские параметры (клавиши, цвета, задержки) должны объявляться через официальный API `Config.Bind`. Это гарантирует совместимость с **BepInEx ConfigurationManager** (окно настроек по `F1`):

```csharp
public static class ModConfig
{
    public static ConfigEntry<KeyCode> PingKey;
    public static ConfigEntry<float> PingDuration;
    public static ConfigEntry<float> CreatureHighlightDuration;
    public static ConfigEntry<Color> DefaultPingColor;

    public static void Init(ConfigFile config)
    {
        PingKey = config.Bind(
            "Controls",
            "PingHotkey",
            KeyCode.Mouse2, // Средняя кнопка мыши
            new ConfigDescription("Клавиша для установки пинга в точку прицела.")
        );

        PingDuration = config.Bind(
            "General",
            "PingDuration",
            20f,
            new ConfigDescription("Длительность отображения 3D-маркера пинга (в секундах).", new AcceptableValueRange<float>(5f, 60f))
        );

        CreatureHighlightDuration = config.Bind(
            "General",
            "CreatureHighlightDuration",
            15f,
            new ConfigDescription("Длительность подсветки силуэта монстра (в секундах).", new AcceptableValueRange<float>(5f, 30f))
        );

        DefaultPingColor = config.Bind(
            "Visuals",
            "DefaultColor",
            new Color(0.2f, 0.8f, 1f, 1f),
            new ConfigDescription("Цвет пинга по умолчанию.")
        );
    }
}
```

---

## 4. Чего делать категорически нельзя («Анти-паттерны»)

1. ❌ **Не использовать `FindObjectsOfType<T>()` в цикле `Update()`**: Это приводит к просадкам FPS. Ссылки должны кэшироваться либо браться через известные события и хитбоксы.
2. ❌ **Не создавать копии материалов через `renderer.material`**: Вызов `renderer.material` инстанциирует новый экземпляр материала в оперативной памяти Unity и приводит к утечкам памяти (memory leak). Для изменения цвета или свечения всегда используется `MaterialPropertyBlock` либо ванильный `MaterialMan`.
3. ❌ **Не хардкодить пути к файлам**: Все пути должны формироваться динамически через `Paths.PluginPath` или `Path.Combine`.
4. ❌ **Не писать сетевой код в обход `ZRoutedRpc`**: Создание собственных сокетов или TCP-соединений ломает NAT-punchthrough и Steam P2P Valheim. Всегда использовать `ZRoutedRpc`.
