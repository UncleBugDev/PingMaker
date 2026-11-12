using System;
using PingMaker.Configuration;
using UnityEngine;

namespace PingMaker.UI
{
    /// <summary>
    /// Внутриигровая панель настроек мода PingMaker.
    /// Открывается по нажатию клавиши ' (русская э).
    /// Позволяет назначить любую клавишу пинга (клавиатура или кнопки мыши Mouse4, Mouse3 и др.),
    /// выбрать личный цвет и настроить параметры отображения.
    /// </summary>
    public class PingSettingsGUI : MonoBehaviour
    {
        public static PingSettingsGUI? Instance { get; private set; }
        public static bool IsOpen => Instance != null && Instance.m_isOpen;

        private bool m_isOpen;
        private bool m_isRebinding;
        private Rect m_windowRect;
        private GUIStyle? m_headerStyle;
        private GUIStyle? m_labelStyle;

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
            float w = 480f;
            float h = 530f;
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

            float w = 480f;
            float h = 530f;
            m_windowRect = new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Close()
        {
            m_isOpen = false;
            m_isRebinding = false;

            if (Player.m_localPlayer != null && !Menu.IsVisible() && !InventoryGui.IsVisible() && !Minimap.IsOpen())
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            ModConfig.Save();
        }

        private void OnGUI()
        {
            if (!m_isOpen) return;

            InitStyles();

            GUI.color = Color.white;
            GUI.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 0.96f);

            m_windowRect = GUI.Window(984321, m_windowRect, DrawWindowContents, "PingMaker — Настройки пинга");
        }

        private void DrawWindowContents(int windowID)
        {
            GUILayout.Space(6f);

            // 1. Controls Section
            GUILayout.Label("1. Кнопка установки пинга", m_headerStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Текущая клавиша:", GUILayout.Width(140f));

            string buttonText = m_isRebinding
                ? ">>> НАЖМИТЕ ЛЮБУЮ КНОПКУ МЫШИ/КЛАВИАТУРЫ <<<"
                : $"[ {FormatKeyCodeName(ModConfig.PingKey.Value)} ] (Кликните для смены)";

            Color prevBg = GUI.backgroundColor;
            if (m_isRebinding)
            {
                GUI.backgroundColor = new Color(1f, 0.85f, 0.2f, 1f);
            }
            else
            {
                GUI.backgroundColor = new Color(0.25f, 0.35f, 0.5f, 1f);
            }

            if (GUILayout.Button(buttonText, GUILayout.Height(32f)))
            {
                m_isRebinding = !m_isRebinding;
            }
            GUI.backgroundColor = prevBg;
            GUILayout.EndHorizontal();

            GUILayout.Space(12f);

            // 2. Personal Color Section
            GUILayout.Label("2. Личный цвет игрока (все ваши метки будут этого цвета)", m_headerStyle);

            int activeIdx = ModConfig.ActiveColorIndex.Value;
            PlayerColorDefinition activeDef = PlayerColors.GetColorById(activeIdx);

            // Preview Box
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Предпросмотр метки:", GUILayout.Width(140f));

            string playerName = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Викинг";
            string hex = ColorUtility.ToHtmlStringRGB(activeDef.Color);
            GUILayout.Label($"<color=#{hex}><b>● {playerName} [{activeDef.Name}] [24m]</b></color>", m_labelStyle);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.Space(6f);

            // 8-Color Grid (4 in row 1, 4 in row 2)
            DrawColorPaletteGrid(activeIdx);

            GUILayout.Space(12f);

            // 3. Gameplay Settings Section
            GUILayout.Label("3. Параметры отображения", m_headerStyle);

            // Ping Duration Slider
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Длительность метки в мире: {Mathf.RoundToInt(ModConfig.PingDuration.Value)} сек", GUILayout.Width(220f));
            ModConfig.PingDuration.Value = GUILayout.HorizontalSlider(ModConfig.PingDuration.Value, 5f, 60f);
            GUILayout.EndHorizontal();

            // Highlight Duration Slider
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Подсветка силуэта монстра: {Mathf.RoundToInt(ModConfig.CreatureHighlightDuration.Value)} сек", GUILayout.Width(220f));
            ModConfig.CreatureHighlightDuration.Value = GUILayout.HorizontalSlider(ModConfig.CreatureHighlightDuration.Value, 5f, 30f);
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Toggles
            ModConfig.ShowDistance.Value = GUILayout.Toggle(ModConfig.ShowDistance.Value, "  Отображать дистанцию в метрах [Xm]");
            ModConfig.ShowOffscreenArrow.Value = GUILayout.Toggle(ModConfig.ShowOffscreenArrow.Value, "  Отображать стрелку-указатель на краю экрана, если метка вне обзора");
            ModConfig.SyncToMinimap.Value = GUILayout.Toggle(ModConfig.SyncToMinimap.Value, "  Дублировать пинг временной точкой на миникарте");

            GUILayout.FlexibleSpace();

            // Close button
            GUI.backgroundColor = new Color(0.2f, 0.6f, 0.35f, 1f);
            if (GUILayout.Button("СОХРАНИТЬ И ЗАКРЫТЬ (или клавиша Esc / ')", GUILayout.Height(36f)))
            {
                Close();
            }
            GUI.backgroundColor = prevBg;

            GUI.DragWindow(new Rect(0, 0, 10000, 24));
        }

        private void DrawColorPaletteGrid(int activeIdx)
        {
            PlayerColorDefinition[] palette = PlayerColors.Palette;

            // Row 1: 0..3
            GUILayout.BeginHorizontal();
            for (int i = 0; i < 4; i++)
            {
                DrawSingleColorButton(palette[i], i == activeIdx);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(4f);

            // Row 2: 4..7
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

            if (GUILayout.Button(label, GUILayout.Height(34f)))
            {
                ModConfig.ActiveColorIndex.Value = def.Id;
                ModConfig.CustomPlayerColor.Value = def.Color;
                ModConfig.Save();

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
            if (m_headerStyle == null)
            {
                m_headerStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    fontSize = 13,
                    normal = { textColor = new Color(1f, 0.85f, 0.35f, 1f) }
                };
            }

            if (m_labelStyle == null)
            {
                m_labelStyle = new GUIStyle(GUI.skin.label)
                {
                    richText = true,
                    fontSize = 13,
                    alignment = TextAnchor.MiddleLeft
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
