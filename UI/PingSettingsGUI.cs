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
    /// Открывается по нажатию клавиши ' (русская э).
    /// Позволяет настроить клавишу пинга (клавиатура или кнопки мыши), выбрать цвет викинга,
    /// а также настроить параметры 3D-меток и трекеров на карте.
    /// </summary>
    public class PingSettingsGUI : MonoBehaviour
    {
        public static PingSettingsGUI? Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.m_isOpen;

        private bool m_isOpen;
        private bool m_isRebinding;
        private Rect m_windowRect;

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
            float w = 520f;
            float h = 650f;
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

            float w = 520f;
            float h = 650f;
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
            GUILayout.Space(4f);

            // Подзаголовок
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label("<color=#D8CFBE>« Личные метки, подсветка и трекеры карты »</color>", m_labelStyle);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(8f);

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

            int activeIdx = ModConfig.ActiveColorIndex.Value;
            PlayerColorDefinition activeDef = PlayerColors.GetColorById(activeIdx);

            // Плашка предпросмотра
            GUILayout.BeginVertical(m_previewBoxStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Предпросмотр:", m_labelStyle, GUILayout.Width(110f));

            string playerName = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Викинг";
            string hex = ColorUtility.ToHtmlStringRGB(activeDef.Color);
            GUILayout.Label($"<color=#{hex}><b>● {playerName} [{activeDef.Name}] [24m]</b>  —  ваша стрелка на карте</color>", m_labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // Сетка цветов
            DrawColorPaletteGrid(activeIdx);

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

            GUILayout.FlexibleSpace();

            // Кнопка сохранения и закрытия
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
        }

        private void DrawSingleColorButton(PlayerColorDefinition def, bool isActive)
        {
            Color prevBg = GUI.backgroundColor;
            Color prevContent = GUI.contentColor;

            GUI.backgroundColor = def.Color;
            GUI.contentColor = (def.Id == 0 || def.Id == 7 || def.Id == 6) ? Color.black : Color.white;

            string label = isActive ? $"✓ {def.Name}" : def.Name;

            if (GUILayout.Button(label, m_buttonStyle, GUILayout.Height(32f)))
            {
                ModConfig.ActiveColorIndex.Value = def.Id;
                ModConfig.CustomPlayerColor.Value = def.Color;
                ModConfig.Save();

                // Синхронизация нового цвета со всеми пирами
                NetworkSync.BroadcastLocalPlayerColor();

                // Мгновенное обновление стрелки на миникарте
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

            GUI.backgroundColor = prevBg;
            GUI.contentColor = prevContent;
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
                    normal = { textColor = new Color(0.95f, 0.85f, 0.45f, 1f) },
                    hover = { textColor = new Color(1f, 1f, 0.7f, 1f) }
                };
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
