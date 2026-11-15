using System;
using PingMaker.Configuration;
using PingMaker.Core;
using PingMaker.Network;
using PingMaker.Visuals;
using UnityEngine;

namespace PingMaker.UI
{
    /// <summary>
    /// Внутриигровая панель настроек мода PingMaker в аутентичном стиле Valheim.
    /// Открывается по нажатию клавиши ' (русская э) или через кнопку меню ESC.
    /// Позволяет настроить клавишу пинга, выбрать любой цвет (8 пресетов викингов или произвольный Color Picker с HEX),
    /// а также настроить параметры 3D-меток, звуки и трекеры на карте с защитой от дубликатов цветов.
    /// </summary>
    public class PingSettingsGUI : MonoBehaviour
    {
        public static PingSettingsGUI? Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.m_isOpen;

        private bool m_isOpen;
        private bool m_isRebinding;
        private Rect m_windowRect;
        private Vector2 m_scrollPosition;

        // Предупреждение о дубликате цвета
        private string? m_duplicateWarning;
        private float m_duplicateWarningTime;

        // Инструмент выбора своего уникального цвета (Color Picker & HEX)
        private bool m_showCustomColorTool;
        private string m_hexInputString = "";
        private float m_hue = 0.10f;
        private float m_sat = 0.90f;
        private float m_val = 0.95f;
        private Color m_pickerColor = new Color(1f, 0.85f, 0.1f);

        // Стили и шрифты в стиле Valheim
        private Font? m_valheimFont;
        private GUIStyle? m_windowStyle;
        private GUIStyle? m_headerStyle;
        private GUIStyle? m_buttonStyle;
        private GUIStyle? m_rebindButtonStyle;
        private GUIStyle? m_previewBoxStyle;
        private GUIStyle? m_labelStyle;
        private GUIStyle? m_toggleStyle;
        private GUIStyle? m_saveButtonStyle;

        public static void EnsureExists()
        {
            if (Instance != null) return;

            GameObject guiObj = new GameObject("PingMaker_SettingsGUI");
            DontDestroyOnLoad(guiObj);
            Instance = guiObj.AddComponent<PingSettingsGUI>();
        }

        private void Awake()
        {
            Instance = this;
            float w = 550f;
            float h = Mathf.Min(840f, Screen.height - 40f);
            m_windowRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);
        }

        private void Update()
        {
            // Открытие / закрытие по клавише настроек (по умолчанию ' / русская э)
            if (Input.GetKeyDown(ModConfig.SettingsKey.Value))
            {
                Toggle();
            }
            else if (m_isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                Close();
            }

            // Удержание курсора мыши разблокированным и видимым каждый кадр пока открыто меню
            if (m_isOpen)
            {
                ZCursor.LockState = CursorLockMode.None;
                ZCursor.Show();
            }

            // Перехват любой клавиши мыши или клавиатуры в режиме переназначения
            if (m_isRebinding)
            {
                foreach (KeyCode kc in (KeyCode[])Enum.GetValues(typeof(KeyCode)))
                {
                    if (Input.GetKeyDown(kc) && kc != KeyCode.None)
                    {
                        if (kc != KeyCode.Escape)
                        {
                            ModConfig.PingKey.Value = kc;
                            ModConfig.Save();

                            if (Player.m_localPlayer != null)
                            {
                                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"[PingMaker] Клавиша пинга назначена на: <b>{FormatKeyCodeName(kc)}</b>");
                            }
                        }
                        m_isRebinding = false;
                        break;
                    }
                }
            }
        }

        public void Toggle()
        {
            if (m_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public void Open()
        {
            m_isOpen = true;
            m_isRebinding = false;
            m_duplicateWarning = null;

            Color active = PlayerColors.GetActiveColor();
            m_pickerColor = active;
            Color.RGBToHSV(active, out m_hue, out m_sat, out m_val);
            m_hexInputString = ColorUtility.ToHtmlStringRGB(active);

            float w = 550f;
            float h = Mathf.Min(840f, Screen.height - 40f);
            m_windowRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            ZCursor.LockState = CursorLockMode.None;
            ZCursor.Show();
        }

        public void Close()
        {
            m_isOpen = false;
            m_isRebinding = false;

            if (Player.m_localPlayer != null && !Menu.IsVisible() && !InventoryGui.IsVisible() && !Minimap.IsOpen())
            {
                ZCursor.LockState = CursorLockMode.Locked;
                ZCursor.Hide();
            }

            ModConfig.Save();
        }

        private void OnGUI()
        {
            if (!m_isOpen) return;

            InitStyles();

            GUI.color = Color.white;
            GUI.backgroundColor = Color.white;

            m_windowRect = GUI.Window(984321, m_windowRect, DrawWindowContents, "ᚱ • НАСТРОЙКИ PINGMAKER • ᛉ", m_windowStyle);
        }

        private void DrawWindowContents(int windowID)
        {
            GUILayout.Space(2f);

            // Подзаголовок
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#D8CFBE>« Личные метки, подсветка и трекеры карты »</color>", m_labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Прокручиваемая область настроек
            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition, GUILayout.ExpandHeight(true));

            // ================= 1. Блок клавиши пинга =================
            GUILayout.Label("  ᚠ  1. КЛАВИША БЫСТРОГО ПИНГА", m_headerStyle);
            GUILayout.Space(4f);

            GUILayout.BeginHorizontal();
            GUILayout.Label("Текущая клавиша:", m_labelStyle, GUILayout.Width(130f));

            string keyText = m_isRebinding
                ? ">>> ᛉ НАЖМИТЕ КНОПКУ МЫШИ / КЛАВИАТУРЫ ᛉ <<<"
                : $"[ ᚱ  {FormatKeyCodeName(ModConfig.PingKey.Value)}  ᚱ ]";

            GUIStyle rebindStyle = m_isRebinding ? m_rebindButtonStyle! : m_buttonStyle!;
            if (GUILayout.Button(keyText, rebindStyle, GUILayout.Height(34f)))
            {
                m_isRebinding = !m_isRebinding;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // ================= 2. Блок выбора цвета =================
            GUILayout.Label("  ᛟ  2. ЛИЧНЫЙ ЦВЕТ ВИКИНГА (МЕТКИ И КАРТА)", m_headerStyle);
            GUILayout.Space(4f);

            // Предупреждение о дубликате цвета (если другой игрок уже занял этот цвет)
            if (!string.IsNullOrEmpty(m_duplicateWarning) && Time.time - m_duplicateWarningTime < 8.0f)
            {
                Color prevBg = GUI.backgroundColor;
                GUI.backgroundColor = new Color(0.85f, 0.25f, 0.25f, 1.0f);
                GUILayout.BeginVertical(m_previewBoxStyle);
                GUILayout.Label($"<color=#FFAAAA><b>⚠ {m_duplicateWarning}</b></color>", m_labelStyle);
                GUILayout.EndVertical();
                GUI.backgroundColor = prevBg;
                GUILayout.Space(4f);
            }

            int activeIdx = ModConfig.ActiveColorIndex.Value;
            Color activeColor = PlayerColors.GetActiveColor();
            string activeHex = ColorUtility.ToHtmlStringRGB(activeColor);
            string activeName = PlayerColors.GetActiveColorName();
            string playerName = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Викинг";

            // Плашка предпросмотра текущего цвета
            GUILayout.BeginVertical(m_previewBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Предпросмотр:", m_labelStyle, GUILayout.Width(110f));
            GUILayout.Label($"<color=#{activeHex}><b>● {playerName} [{activeName}] [24m]</b>  —  ваша стрелка и пинг</color>", m_labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // Сетка стандартных пресетов викингов (8 базовых + 9-й слот свой цвет)
            DrawColorPaletteGrid(activeIdx);

            GUILayout.Space(6f);

            // Кнопка переключения Color Picker Tool
            string pickerToggleText = m_showCustomColorTool
                ? "▲  СВЕРНУТЬ COLOR PICKER (ИНСТРУМЕНТ ВЫБОРА ЦВЕТА)"
                : "▼  ОТКРЫТЬ COLOR PICKER (ПОЛЗУНКИ ОТТЕНКА И ВВОД HEX-КОДА)";

            if (GUILayout.Button(pickerToggleText, m_buttonStyle, GUILayout.Height(28f)))
            {
                m_showCustomColorTool = !m_showCustomColorTool;
                if (m_showCustomColorTool)
                {
                    m_pickerColor = activeColor;
                    Color.RGBToHSV(activeColor, out m_hue, out m_sat, out m_val);
                    m_hexInputString = activeHex;
                }
            }

            if (m_showCustomColorTool)
            {
                DrawCustomColorTool();
            }

            GUILayout.Space(10f);

            // ================= 3. Блок параметров отображения =================
            GUILayout.Label("  ᛏ  3. ПАРАМЕТРЫ ОТОБРАЖЕНИЯ", m_headerStyle);
            GUILayout.Space(4f);

            // Длительность пинга в мире
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Длительность 3D-метки: <color=#E5B85C><b>{Mathf.RoundToInt(ModConfig.PingDuration.Value)} сек</b></color>", m_labelStyle, GUILayout.Width(220f));
            ModConfig.PingDuration.Value = GUILayout.HorizontalSlider(ModConfig.PingDuration.Value, 5f, 60f);
            GUILayout.EndHorizontal();

            // Подсветка силуэта
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Подсветка монстров: <color=#E5B85C><b>{Mathf.RoundToInt(ModConfig.CreatureHighlightDuration.Value)} сек</b></color>", m_labelStyle, GUILayout.Width(220f));
            ModConfig.CreatureHighlightDuration.Value = GUILayout.HorizontalSlider(ModConfig.CreatureHighlightDuration.Value, 5f, 30f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Чекбоксы
            ModConfig.ShowDistance.Value = GUILayout.Toggle(ModConfig.ShowDistance.Value, "  Отображать дистанцию до цели [Xm]", m_toggleStyle);
            ModConfig.ShowOffscreenArrow.Value = GUILayout.Toggle(ModConfig.ShowOffscreenArrow.Value, "  Отображать стрелку на краю экрана, если метка вне обзора", m_toggleStyle);
            ModConfig.SyncToMinimap.Value = GUILayout.Toggle(ModConfig.SyncToMinimap.Value, "  Дублировать пинг временной отметкой на миникарте", m_toggleStyle);
            ModConfig.ColorizeMapPlayers.Value = GUILayout.Toggle(ModConfig.ColorizeMapPlayers.Value, "  Окрашивать стрелки направления игроков на карте в их личный цвет", m_toggleStyle);
            ModConfig.EnableRadialColorMenu.Value = GUILayout.Toggle(ModConfig.EnableRadialColorMenu.Value, "  Круговое радиальное меню цвета при удержании", m_toggleStyle);

            GUILayout.Space(6f);

            // Режим работы прицела захвата цели
            GUILayout.BeginHorizontal();
            GUILayout.Label("Режим прицела захвата:", m_labelStyle, GUILayout.Width(180f));
            string modeName = ModConfig.TargetReticleMode.Value switch
            {
                1 => "Только при метке на существо",
                2 => "Только при наведении прицела",
                _ => "При наведении и метке (Стандарт)"
            };
            if (GUILayout.Button($"[  {modeName}  ]", m_buttonStyle, GUILayout.Height(28f)))
            {
                ModConfig.TargetReticleMode.Value = (ModConfig.TargetReticleMode.Value + 1) % 3;
            }
            GUILayout.EndHorizontal();

            // Стиль стрелочки захвата
            GUILayout.BeginHorizontal();
            GUILayout.Label("Стиль стрелки захвата:", m_labelStyle, GUILayout.Width(180f));
            string styleName = ModConfig.ReticleArrowStyle.Value == 0
                ? "↘ Диагональная (по умолчанию)"
                : "↓ Сверху";
            if (GUILayout.Button($"[  {styleName}  ]", m_buttonStyle, GUILayout.Height(28f)))
            {
                ModConfig.ReticleArrowStyle.Value = ModConfig.ReticleArrowStyle.Value == 0 ? 1 : 0;
            }
            GUILayout.EndHorizontal();

            // ================= 4. Блок настроек звука =================
            GUILayout.Space(8f);
            GUILayout.Label("  ᚢ  4. НАСТРОЙКИ ЗВУКА И ГРОМКОСТИ", m_headerStyle);
            GUILayout.Space(4f);

            // Выбор стиля звука с кнопкой прослушивания
            GUILayout.BeginHorizontal();
            GUILayout.Label("Стиль звука пинга:", m_labelStyle, GUILayout.Width(160f));
            string soundPresetName = ProceduralAudio.GetPresetName(ModConfig.PingSoundPreset.Value);
            if (GUILayout.Button($"[  {soundPresetName}  ]", m_buttonStyle, GUILayout.Height(28f)))
            {
                ModConfig.PingSoundPreset.Value = (ModConfig.PingSoundPreset.Value + 1) % ProceduralAudio.PresetCount;
                ProceduralAudio.PlayPreview(ModConfig.PingSoundPreset.Value, ModConfig.MyPingVolume.Value * ModConfig.AudioVolume.Value);
            }
            if (GUILayout.Button("▶", m_buttonStyle, GUILayout.Width(34f), GUILayout.Height(28f)))
            {
                ProceduralAudio.PlayPreview(ModConfig.PingSoundPreset.Value, ModConfig.MyPingVolume.Value * ModConfig.AudioVolume.Value);
            }
            GUILayout.EndHorizontal();

            // Ползунок громкости своих пингов
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Громкость моих пингов: <color=#E5B85C><b>{Mathf.RoundToInt(ModConfig.MyPingVolume.Value * 100f)}%</b></color>", m_labelStyle, GUILayout.Width(220f));
            ModConfig.MyPingVolume.Value = GUILayout.HorizontalSlider(ModConfig.MyPingVolume.Value, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            // Ползунок громкости пингов друзей
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Громкость пингов друзей: <color=#E5B85C><b>{Mathf.RoundToInt(ModConfig.OthersPingVolume.Value * 100f)}%</b></color>", m_labelStyle, GUILayout.Width(220f));
            ModConfig.OthersPingVolume.Value = GUILayout.HorizontalSlider(ModConfig.OthersPingVolume.Value, 0.0f, 1.0f);
            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            // Чекбокс глушения других игроков
            ModConfig.MuteOtherPlayersPings.Value = GUILayout.Toggle(ModConfig.MuteOtherPlayersPings.Value, "  Полностью отключить звуки пингов от других игроков", m_toggleStyle);

            GUILayout.EndScrollView();

            GUILayout.Space(6f);

            // Закрепленная кнопка сохранения и закрытия внизу окна
            if (GUILayout.Button("ᛉ  СОХРАНИТЬ И ЗАКРЫТЬ  (Esc / ')  ᛉ", m_saveButtonStyle, GUILayout.Height(38f)))
            {
                Close();
            }

            GUI.DragWindow(new Rect(0, 0, 10000, 28));
        }

        private void DrawColorPaletteGrid(int activeIdx)
        {
            PlayerColorDefinition[] palette = PlayerColors.Palette;

            // Ряд 1: 0..3
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 4; i++)
            {
                DrawSingleColorButton(palette[i], i == activeIdx);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Ряд 2: 4..7
            GUILayout.BeginHorizontal();
            for (int i = 4; i < 8; i++)
            {
                DrawSingleColorButton(palette[i], i == activeIdx);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Ряд 3: 9-я кнопка «Свой цвет»
            bool isCustomActive = (activeIdx == -1);
            Color customCol = ModConfig.CustomPlayerColor.Value;
            string customHex = ColorUtility.ToHtmlStringRGB(customCol);
            string customLabel = isCustomActive
                ? $"✓ Свой уникальный цвет (#{customHex})"
                : $"★ Свой уникальный цвет (#{customHex})";

            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;

            GUI.backgroundColor = customCol;
            float customBrightness = (customCol.r * 0.299f + customCol.g * 0.587f + customCol.b * 0.114f);
            GUI.contentColor = customBrightness > 0.55f ? Color.black : Color.white;

            if (GUILayout.Button(customLabel, m_buttonStyle, GUILayout.Height(32f)))
            {
                TryApplyCustomColor(customCol);
                m_showCustomColorTool = true;
            }

            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
        }

        private void DrawSingleColorButton(PlayerColorDefinition def, bool isActive)
        {
            bool isTaken = NetworkSync.IsColorTakenByOtherPlayer(def.Color, def.Id, out string takenByName);

            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;

            GUI.backgroundColor = def.Color;
            GUI.contentColor = (def.Id == 0 || def.Id == 7 || def.Id == 6) ? Color.black : Color.white;

            string label;
            if (isTaken)
            {
                label = $"{def.Name}\n<size=9><color=#FF5555>[Занят: {takenByName}]</color></size>";
            }
            else if (isActive)
            {
                label = $"✓ {def.Name}";
            }
            else
            {
                label = def.Name;
            }

            if (GUILayout.Button(label, m_buttonStyle, GUILayout.Height(34f)))
            {
                TryApplyPreset(def);
            }

            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
        }

        private void DrawCustomColorTool()
        {
            GUILayout.Space(4f);
            GUILayout.BeginVertical(m_previewBoxStyle);

            GUILayout.Label("<color=#E5B85C><b>ИНСТРУМЕНТ НАСТРОЙКИ СВОЕГО ЦВЕТА (HSV / HEX):</b></color>", m_labelStyle);
            GUILayout.Space(4f);

            // 1. Поле ввода HEX
            GUILayout.BeginHorizontal();
            GUILayout.Label("HEX-код цвета:", m_labelStyle, GUILayout.Width(100f));
            GUILayout.Label("#", m_labelStyle, GUILayout.Width(10f));
            string typedHex = GUILayout.TextField(m_hexInputString, 8, GUILayout.Width(80f), GUILayout.Height(24f));
            if (typedHex != m_hexInputString)
            {
                m_hexInputString = typedHex.Trim().ToUpperInvariant();
                if (ColorUtility.TryParseHtmlString("#" + m_hexInputString.TrimStart('#'), out Color parsed))
                {
                    m_pickerColor = parsed;
                    Color.RGBToHSV(parsed, out m_hue, out m_sat, out m_val);
                }
            }

            if (GUILayout.Button("Вставить", m_buttonStyle, GUILayout.Width(75f), GUILayout.Height(24f)))
            {
                string clip = GUIUtility.systemCopyBuffer != null ? GUIUtility.systemCopyBuffer.Trim() : "";
                if (!string.IsNullOrEmpty(clip) && ColorUtility.TryParseHtmlString("#" + clip.TrimStart('#'), out Color parsed))
                {
                    m_pickerColor = parsed;
                    Color.RGBToHSV(parsed, out m_hue, out m_sat, out m_val);
                    m_hexInputString = ColorUtility.ToHtmlStringRGB(parsed);
                }
            }

            if (GUILayout.Button("Копировать", m_buttonStyle, GUILayout.Width(90f), GUILayout.Height(24f)))
            {
                GUIUtility.systemCopyBuffer = "#" + ColorUtility.ToHtmlStringRGB(m_pickerColor);
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"[PingMaker] Скопирован код #{ColorUtility.ToHtmlStringRGB(m_pickerColor)}");
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 2. Ползунки HSV
            // Оттенок
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Оттенок (Hue): <color=#E5B85C><b>{Mathf.RoundToInt(m_hue * 360f)}°</b></color>", m_labelStyle, GUILayout.Width(170f));
            float newHue = GUILayout.HorizontalSlider(m_hue, 0.0f, 1.0f);
            if (!Mathf.Approximately(newHue, m_hue))
            {
                m_hue = newHue;
                m_pickerColor = Color.HSVToRGB(m_hue, m_sat, m_val);
                m_hexInputString = ColorUtility.ToHtmlStringRGB(m_pickerColor);
            }
            GUILayout.EndHorizontal();

            // Насыщенность
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Насыщенность: <color=#E5B85C><b>{Mathf.RoundToInt(m_sat * 100f)}%</b></color>", m_labelStyle, GUILayout.Width(170f));
            float newSat = GUILayout.HorizontalSlider(m_sat, 0.0f, 1.0f);
            if (!Mathf.Approximately(newSat, m_sat))
            {
                m_sat = newSat;
                m_pickerColor = Color.HSVToRGB(m_hue, m_sat, m_val);
                m_hexInputString = ColorUtility.ToHtmlStringRGB(m_pickerColor);
            }
            GUILayout.EndHorizontal();

            // Яркость
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Яркость (Val): <color=#E5B85C><b>{Mathf.RoundToInt(m_val * 100f)}%</b></color>", m_labelStyle, GUILayout.Width(170f));
            float newVal = GUILayout.HorizontalSlider(m_val, 0.20f, 1.0f);
            if (!Mathf.Approximately(newVal, m_val))
            {
                m_val = newVal;
                m_pickerColor = Color.HSVToRGB(m_hue, m_sat, m_val);
                m_hexInputString = ColorUtility.ToHtmlStringRGB(m_pickerColor);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6f);

            // 3. Плашка предпросмотра настраиваемого цвета + кнопка выбора
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;

            GUI.backgroundColor = m_pickerColor;
            float brightness = (m_pickerColor.r * 0.299f + m_pickerColor.g * 0.587f + m_pickerColor.b * 0.114f);
            GUI.contentColor = brightness > 0.55f ? Color.black : Color.white;

            string applyText = $"ᛉ  ВЫБРАТЬ И СОХРАНИТЬ ЭТОТ ЦВЕТ  (#{m_hexInputString})  ᛉ";
            if (GUILayout.Button(applyText, m_buttonStyle, GUILayout.Height(34f)))
            {
                TryApplyCustomColor(m_pickerColor);
            }

            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;

            GUILayout.EndVertical();
        }

        private void TryApplyPreset(PlayerColorDefinition def)
        {
            if (NetworkSync.IsColorTakenByOtherPlayer(def.Color, def.Id, out string takenByName))
            {
                m_duplicateWarning = $"К сожалению, цвет «{def.Name}» уже выбран игроком {takenByName}!\nПожалуйста, выберите другой цвет, чтобы не путаться на карте.";
                m_duplicateWarningTime = Time.time;
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#FF5555>Цвет «{def.Name}» уже занят игроком {takenByName}!</color>\nВыберите другой цвет.");
                }
                return;
            }

            m_duplicateWarning = null;
            ModConfig.ActiveColorIndex.Value = def.Id;
            ModConfig.CustomPlayerColor.Value = def.Color;
            if (ModConfig.CustomColorHex != null)
            {
                ModConfig.CustomColorHex.Value = ColorUtility.ToHtmlStringRGB(def.Color);
            }
            ModConfig.Save();

            NetworkSync.BroadcastLocalPlayerColor();

            if (Minimap.instance != null)
            {
                PlayerMapTracker.UpdateLocalPlayer(Minimap.instance);
            }

            if (Player.m_localPlayer != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(def.Color);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"[PingMaker] Выбран личный цвет: <color=#{hex}><b>{def.Name}</b></color>");
            }
        }

        private void TryApplyCustomColor(Color col)
        {
            if (NetworkSync.IsColorTakenByOtherPlayer(col, -1, out string takenByName))
            {
                string hex = ColorUtility.ToHtmlStringRGB(col);
                m_duplicateWarning = $"К сожалению, оттенок #{hex} уже выбран игроком {takenByName}!\nПожалуйста, сдвиньте оттенок на другой.";
                m_duplicateWarningTime = Time.time;
                if (Player.m_localPlayer != null)
                {
                    Player.m_localPlayer.Message(MessageHud.MessageType.Center, $"<color=#FF5555>Цвет #{hex} уже занят игроком {takenByName}!</color>\nВыберите другой оттенок.");
                }
                return;
            }

            m_duplicateWarning = null;
            ModConfig.ActiveColorIndex.Value = -1;
            ModConfig.CustomPlayerColor.Value = col;
            if (ModConfig.CustomColorHex != null)
            {
                ModConfig.CustomColorHex.Value = ColorUtility.ToHtmlStringRGB(col);
            }
            ModConfig.Save();

            NetworkSync.BroadcastLocalPlayerColor();

            if (Minimap.instance != null)
            {
                PlayerMapTracker.UpdateLocalPlayer(Minimap.instance);
            }

            if (Player.m_localPlayer != null)
            {
                string hex = ColorUtility.ToHtmlStringRGB(col);
                Player.m_localPlayer.Message(MessageHud.MessageType.TopLeft, $"[PingMaker] Выбран свой уникальный цвет: <color=#{hex}><b>#{hex}</b></color>");
            }
        }

        private string FormatKeyCodeName(KeyCode kc)
        {
            switch (kc)
            {
                case KeyCode.Mouse0: return "Левая кнопка мыши (ЛКМ)";
                case KeyCode.Mouse1: return "Правая кнопка мыши (ПКМ)";
                case KeyCode.Mouse2: return "Колёсико мыши (СКМ)";
                case KeyCode.Mouse3: return "Боковая кнопка мыши 3";
                case KeyCode.Mouse4: return "Боковая кнопка мыши 4";
                case KeyCode.Mouse5: return "Боковая кнопка мыши 5";
                case KeyCode.Mouse6: return "Боковая кнопка мыши 6";
                case KeyCode.Quote: return "' (русская э)";
                case KeyCode.BackQuote: return "~ (русская ё)";
                default: return kc.ToString();
            }
        }

        private void InitStyles()
        {
            if (m_valheimFont == null)
            {
                // Поиск оригинального шрифта Valheim в загруженных ресурсах
                Font[] allFonts = Resources.FindObjectsOfTypeAll<Font>();
                for (int i = 0; i < allFonts.Length; i++)
                {
                    Font f = allFonts[i];
                    if (f != null && (f.name.Contains("Averia") || f.name.Contains("Norse") || f.name.Contains("Valheim")))
                    {
                        m_valheimFont = f;
                        break;
                    }
                }
            }

            if (m_windowStyle == null)
            {
                m_windowStyle = new GUIStyle(GUI.skin.window)
                {
                    normal = { background = ProceduralIcons.GetVikingWindowTexture(), textColor = new Color(0.95f, 0.8f, 0.45f, 1f) },
                    onNormal = { background = ProceduralIcons.GetVikingWindowTexture(), textColor = new Color(0.95f, 0.8f, 0.45f, 1f) },
                    border = new RectOffset(14, 14, 14, 14),
                    padding = new RectOffset(16, 16, 20, 16),
                    font = m_valheimFont,
                    fontStyle = FontStyle.Bold,
                    fontSize = 15,
                    alignment = TextAnchor.UpperCenter
                };
            }

            if (m_headerStyle == null)
            {
                m_headerStyle = new GUIStyle(GUI.skin.label)
                {
                    normal = { background = ProceduralIcons.GetVikingBannerTexture(), textColor = new Color(1f, 0.88f, 0.52f, 1f) },
                    border = new RectOffset(6, 6, 6, 6),
                    padding = new RectOffset(10, 10, 5, 5),
                    font = m_valheimFont,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (m_buttonStyle == null)
            {
                m_buttonStyle = new GUIStyle(GUI.skin.button)
                {
                    normal = { background = ProceduralIcons.GetVikingButtonTexture(false, false), textColor = new Color(0.92f, 0.88f, 0.82f, 1f) },
                    hover = { background = ProceduralIcons.GetVikingButtonTexture(true, false), textColor = new Color(1f, 0.95f, 0.6f, 1f) },
                    active = { background = ProceduralIcons.GetVikingButtonTexture(false, true), textColor = Color.white },
                    border = new RectOffset(8, 8, 8, 8),
                    font = m_valheimFont,
                    fontStyle = FontStyle.Bold,
                    fontSize = 12,
                    richText = true,
                    alignment = TextAnchor.MiddleCenter
                };
            }

            if (m_rebindButtonStyle == null)
            {
                m_rebindButtonStyle = new GUIStyle(m_buttonStyle)
                {
                    normal = { background = ProceduralIcons.GetVikingButtonTexture(true, false), textColor = new Color(1f, 0.9f, 0.2f, 1f) },
                    fontStyle = FontStyle.Bold
                };
            }

            if (m_previewBoxStyle == null)
            {
                m_previewBoxStyle = new GUIStyle(GUI.skin.box)
                {
                    normal = { background = ProceduralIcons.GetVikingPreviewBoxTexture() },
                    border = new RectOffset(6, 6, 6, 6),
                    padding = new RectOffset(10, 10, 6, 6)
                };
            }

            if (m_labelStyle == null)
            {
                m_labelStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    font = m_valheimFont,
                    fontSize = 12,
                    normal = { textColor = new Color(0.9f, 0.85f, 0.78f, 1f) },
                    alignment = TextAnchor.MiddleLeft
                };
            }

            if (m_toggleStyle == null)
            {
                m_toggleStyle = new GUIStyle(GUI.skin.toggle)
                {
                    font = m_valheimFont,
                    fontSize = 12,
                    normal = { textColor = new Color(0.88f, 0.84f, 0.78f, 1f) }
                };
            }

            if (m_saveButtonStyle == null)
            {
                m_saveButtonStyle = new GUIStyle(m_buttonStyle)
                {
                    fontSize = 13,
                    fontStyle = FontStyle.Bold,
                    normal = { background = ProceduralIcons.GetVikingButtonTexture(true, false), textColor = new Color(1f, 0.95f, 0.6f, 1f) }
                };
            }
        }
    }
}
