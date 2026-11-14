using BepInEx.Configuration;
using UnityEngine;

namespace PingMaker.Configuration
{
    public static class ModConfig
    {
        // Controls
        public static ConfigEntry<KeyCode> PingKey = null!;
        public static ConfigEntry<KeyCode> AlternativePingKey = null!;
        public static ConfigEntry<KeyCode> SettingsKey = null!;
        public static ConfigEntry<float> HoldTimeForMenu = null!;
        public static ConfigEntry<bool> EnableRadialColorMenu = null!;

        // Personal Color Settings
        public static ConfigEntry<int> ActiveColorIndex = null!;
        public static ConfigEntry<Color> CustomPlayerColor = null!;

        // Gameplay & Timers
        public static ConfigEntry<float> PingDuration = null!;
        public static ConfigEntry<float> CreatureHighlightDuration = null!;
        public static ConfigEntry<float> MaxPingDistance = null!;
        public static ConfigEntry<int> MaxActivePingsPerPlayer = null!;
        public static ConfigEntry<bool> SyncToMinimap = null!;

        // Visuals & Display
        public static ConfigEntry<bool> ShowDistance = null!;
        public static ConfigEntry<bool> ShowPlayerName = null!;
        public static ConfigEntry<bool> ShowOffscreenArrow = null!;
        public static ConfigEntry<bool> ColorizeMapPlayers = null!;
        public static ConfigEntry<int> TargetReticleMode = null!;
        public static ConfigEntry<int> ReticleArrowStyle = null!;
        public static ConfigEntry<float> MarkerScale = null!;
        public static ConfigEntry<float> AudioVolume = null!;

        public static void Init(ConfigFile config)
        {
            // Controls
            PingKey = config.Bind(
                "1. Controls",
                "PingKey",
                KeyCode.Mouse4,
                new ConfigDescription("Основная клавиша для установки пинга (по умолчанию: Mouse4 / Боковая кнопка мыши).")
            );

            AlternativePingKey = config.Bind(
                "1. Controls",
                "AlternativePingKey",
                KeyCode.None,
                new ConfigDescription("Дополнительная клавиша для пинга (по умолчанию: None, чтобы избежать конфликтов).")
            );

            SettingsKey = config.Bind(
                "1. Controls",
                "SettingsKey",
                KeyCode.Quote,
                new ConfigDescription("Клавиша открытия панели настроек мода PingMaker (по умолчанию: ' / русская э).")
            );

            HoldTimeForMenu = config.Bind(
                "1. Controls",
                "HoldTimeForMenu",
                0.20f,
                new ConfigDescription("Время удержания клавиши (в секундах) для открытия радиального меню выбора личного цвета.", new AcceptableValueRange<float>(0.1f, 1.0f))
            );

            EnableRadialColorMenu = config.Bind(
                "1. Controls",
                "EnableRadialColorMenu",
                false,
                new ConfigDescription("Включать ли круговое меню выбора цвета при удержании клавиши пинга (по умолчанию выключено, чтобы избежать случайных смен цвета во время боя или эмоций; цвет настраивается через панель на клавишу ').")
            );

            // Personal Color
            ActiveColorIndex = config.Bind(
                "2. Personal Color",
                "ActiveColorIndex",
                0, // 0 = Yellow by default
                new ConfigDescription("Индекс выбранного личного цвета игрока в палитре (0=Жёлтый, 1=Оранжевый, 2=Красный, 3=Розовый, 4=Фиолетовый, 5=Синий, 6=Голубой, 7=Лайм).", new AcceptableValueRange<int>(0, 7))
            );

            CustomPlayerColor = config.Bind(
                "2. Personal Color",
                "CustomPlayerColor",
                new Color(1.00f, 0.90f, 0.05f, 1.0f),
                new ConfigDescription("Пользовательский цвет игрока, если не используется стандартная палитра.")
            );

            // General & Durations
            PingDuration = config.Bind(
                "3. General",
                "PingDuration",
                20.0f,
                new ConfigDescription("Длительность отображения 3D-маркера в мире (в секундах).", new AcceptableValueRange<float>(5.0f, 60.0f))
            );

            CreatureHighlightDuration = config.Bind(
                "3. General",
                "CreatureHighlightDuration",
                18.0f,
                new ConfigDescription("Длительность подсветки силуэта обнаруженного монстра (в секундах).", new AcceptableValueRange<float>(5.0f, 30.0f))
            );

            MaxPingDistance = config.Bind(
                "3. General",
                "MaxPingDistance",
                1000.0f,
                new ConfigDescription("Максимальная дальность луча прицела для установки пинга (в метрах).", new AcceptableValueRange<float>(100.0f, 3000.0f))
            );

            MaxActivePingsPerPlayer = config.Bind(
                "3. General",
                "MaxActivePingsPerPlayer",
                3,
                new ConfigDescription("Максимальное количество одновременно активных меток от одного игрока.", new AcceptableValueRange<int>(1, 10))
            );

            SyncToMinimap = config.Bind(
                "3. General",
                "SyncToMinimap",
                true,
                new ConfigDescription("Дублировать ли пинг временной отметкой на миникарте и большой карте цветом игрока.")
            );

            // Visuals
            ShowDistance = config.Bind(
                "4. Visuals",
                "ShowDistance",
                true,
                new ConfigDescription("Отображать ли динамическое расстояние в метрах под маркером.")
            );

            ShowPlayerName = config.Bind(
                "4. Visuals",
                "ShowPlayerName",
                true,
                new ConfigDescription("Отображать ли никнейм игрока, поставившего пинг.")
            );

            ShowOffscreenArrow = config.Bind(
                "4. Visuals",
                "ShowOffscreenArrow",
                true,
                new ConfigDescription("Отображать ли направляющую стрелку на краю экрана, если маркер вне поля зрения.")
            );

            ColorizeMapPlayers = config.Bind(
                "4. Visuals",
                "ColorizeMapPlayers",
                true,
                new ConfigDescription("Окрашивать ли трекеры и стрелки направления игроков на карте и миникарте в их выбранный личный цвет.")
            );

            TargetReticleMode = config.Bind(
                "4. Visuals",
                "TargetReticleMode",
                0, // 0 = Both, 1 = OnPingOnly, 2 = OnHoverOnly
                new ConfigDescription("Режим работы тактического прицела захвата цели (0 = И при наведении, и при установке метки [по умолчанию], 1 = Только при установке метки на существо, 2 = Только при наведении прицела).", new AcceptableValueRange<int>(0, 2))
            );

            ReticleArrowStyle = config.Bind(
                "4. Visuals",
                "ReticleArrowStyle",
                0, // 0 = Diagonal (сверху-справа, как на рисунке), 1 = Top (сверху)
                new ConfigDescription("Стиль стрелочки захвата цели (0 = Диагональная стрелка с верхнего правого угла [по умолчанию], 1 = Стрелка строго сверху).", new AcceptableValueRange<int>(0, 1))
            );

            MarkerScale = config.Bind(
                "4. Visuals",
                "MarkerScale",
                1.0f,
                new ConfigDescription("Масштаб 3D-маркеров на экране.", new AcceptableValueRange<float>(0.5f, 2.0f))
            );

            AudioVolume = config.Bind(
                "4. Visuals",
                "AudioVolume",
                1.0f,
                new ConfigDescription("Громкость звукового сигнала при установке пинга.", new AcceptableValueRange<float>(0.0f, 1.0f))
            );
        }

        public static void Save()
        {
            PingMakerPlugin.Instance?.Config.Save();
        }
    }
}
