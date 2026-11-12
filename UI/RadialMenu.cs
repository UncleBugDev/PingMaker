using System;
using System.Collections.Generic;
using PingMaker.Configuration;
using PingMaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.UI
{
    /// <summary>
    /// Круговое радиальное меню выбора личного цвета игрока.
    /// Открывается при удержании клавиши пинга (> 0.20 сек).
    /// Выбранный цвет становится активным для всех последующих пингов игрока и сохраняется в конфиге.
    /// </summary>
    public class RadialMenu : MonoBehaviour
    {
        public static RadialMenu? Instance { get; private set; }

        private bool m_isOpen;
        private Vector2 m_centerPos;
        private int m_hoveredIndex = -1;

        private GameObject m_menuRoot = null!;
        private CanvasGroup m_canvasGroup = null!;
        private Text m_centerTitle = null!;
        private Text m_centerPlayerName = null!;
        private Text m_centerColorLabel = null!;

        private readonly List<ColorSectorItem> m_sectors = new List<ColorSectorItem>();

        private class ColorSectorItem
        {
            public int Index;
            public PlayerColorDefinition Definition;
            public RectTransform Rect = null!;
            public Image CircleImage = null!;
            public Image CheckmarkImage = null!;
            public Vector2 Direction;
        }

        public bool IsOpen => m_isOpen;

        public static void EnsureExists(Transform parent)
        {
            if (Instance != null) return;

            GameObject menuObj = new GameObject("PingMaker_RadialMenu");
            menuObj.transform.SetParent(parent, false);
            Instance = menuObj.AddComponent<RadialMenu>();
        }

        private void Awake()
        {
            Instance = this;
            BuildUI();
            Hide();
        }

        private void BuildUI()
        {
            m_menuRoot = new GameObject("ColorMenuContainer", typeof(RectTransform), typeof(CanvasGroup));
            m_menuRoot.transform.SetParent(transform, false);

            RectTransform rootRect = m_menuRoot.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(400f, 400f);
            rootRect.anchoredPosition = Vector2.zero;

            m_canvasGroup = m_menuRoot.GetComponent<CanvasGroup>();

            // Outer Ring Background
            GameObject ringObj = new GameObject("OuterRing", typeof(RectTransform), typeof(Image));
            ringObj.transform.SetParent(m_menuRoot.transform, false);
            RectTransform ringRect = ringObj.GetComponent<RectTransform>();
            ringRect.sizeDelta = new Vector2(300f, 300f);
            Image ringImg = ringObj.GetComponent<Image>();
            ringImg.sprite = ProceduralIcons.GetColorPaletteRingSprite();

            Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

            // Center Info Container
            GameObject centerObj = new GameObject("CenterDisplay", typeof(RectTransform));
            centerObj.transform.SetParent(m_menuRoot.transform, false);
            RectTransform centerRect = centerObj.GetComponent<RectTransform>();
            centerRect.sizeDelta = new Vector2(160f, 120f);
            centerRect.anchoredPosition = Vector2.zero;

            // 1. Title ("ЛИЧНЫЙ ЦВЕТ")
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(Text), typeof(Outline));
            titleObj.transform.SetParent(centerObj.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.sizeDelta = new Vector2(160f, 18f);
            tRect.anchoredPosition = new Vector2(0f, 26f);
            m_centerTitle = titleObj.GetComponent<Text>();
            m_centerTitle.font = font;
            m_centerTitle.fontSize = 11;
            m_centerTitle.fontStyle = FontStyle.Normal;
            m_centerTitle.alignment = TextAnchor.MiddleCenter;
            m_centerTitle.text = "ЛИЧНЫЙ ЦВЕТ";
            m_centerTitle.color = new Color(0.8f, 0.85f, 0.9f, 0.8f);
            titleObj.GetComponent<Outline>().effectColor = Color.black;

            // 2. Player Nickname Preview in selected color
            GameObject nameObj = new GameObject("PlayerName", typeof(RectTransform), typeof(Text), typeof(Outline));
            nameObj.transform.SetParent(centerObj.transform, false);
            RectTransform nRect = nameObj.GetComponent<RectTransform>();
            nRect.sizeDelta = new Vector2(160f, 24f);
            nRect.anchoredPosition = new Vector2(0f, 4f);
            m_centerPlayerName = nameObj.GetComponent<Text>();
            m_centerPlayerName.font = font;
            m_centerPlayerName.fontSize = 15;
            m_centerPlayerName.fontStyle = FontStyle.Bold;
            m_centerPlayerName.alignment = TextAnchor.MiddleCenter;
            m_centerPlayerName.text = "Викинг";
            m_centerPlayerName.color = Color.yellow;
            nameObj.GetComponent<Outline>().effectColor = Color.black;

            // 3. Color Name Label
            GameObject colorObj = new GameObject("ColorName", typeof(RectTransform), typeof(Text), typeof(Outline));
            colorObj.transform.SetParent(centerObj.transform, false);
            RectTransform cRect = colorObj.GetComponent<RectTransform>();
            cRect.sizeDelta = new Vector2(160f, 20f);
            cRect.anchoredPosition = new Vector2(0f, -20f);
            m_centerColorLabel = colorObj.GetComponent<Text>();
            m_centerColorLabel.font = font;
            m_centerColorLabel.fontSize = 12;
            m_centerColorLabel.fontStyle = FontStyle.Bold;
            m_centerColorLabel.alignment = TextAnchor.MiddleCenter;
            m_centerColorLabel.text = "Жёлтый";
            m_centerColorLabel.color = Color.white;
            colorObj.GetComponent<Outline>().effectColor = Color.black;

            // Generate Color Slices
            float radius = 105f;
            int count = PlayerColors.Palette.Length;
            float stepAngle = 360f / count;

            Sprite circleSprite = ProceduralIcons.GetColorCircleSprite();
            Sprite checkmarkSprite = ProceduralIcons.GetCheckmarkSprite();

            for (int i = 0; i < count; i++)
            {
                PlayerColorDefinition def = PlayerColors.Palette[i];
                float angleDeg = 90f - (i * stepAngle);
                float rad = angleDeg * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                Vector2 pos = dir * radius;

                GameObject itemObj = new GameObject($"Color_{def.Name}", typeof(RectTransform), typeof(Image));
                itemObj.transform.SetParent(m_menuRoot.transform, false);
                RectTransform iRect = itemObj.GetComponent<RectTransform>();
                iRect.sizeDelta = new Vector2(46f, 46f);
                iRect.anchoredPosition = pos;

                Image circleImg = itemObj.GetComponent<Image>();
                circleImg.sprite = circleSprite;
                circleImg.color = def.Color;

                // Checkmark for active color
                GameObject checkObj = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
                checkObj.transform.SetParent(itemObj.transform, false);
                RectTransform checkRect = checkObj.GetComponent<RectTransform>();
                checkRect.sizeDelta = new Vector2(26f, 26f);
                checkRect.anchoredPosition = Vector2.zero;

                Image checkImg = checkObj.GetComponent<Image>();
                checkImg.sprite = checkmarkSprite;
                checkImg.color = Color.black;
                checkObj.SetActive(false);

                ColorSectorItem sector = new ColorSectorItem
                {
                    Index = i,
                    Definition = def,
                    Rect = iRect,
                    CircleImage = circleImg,
                    CheckmarkImage = checkImg,
                    Direction = dir
                };

                m_sectors.Add(sector);
            }
        }

        public void Open(Vector2 screenCenter)
        {
            m_isOpen = true;
            m_centerPos = screenCenter;
            m_hoveredIndex = ModConfig.ActiveColorIndex.Value;

            RectTransform rootRect = m_menuRoot.GetComponent<RectTransform>();
            rootRect.position = screenCenter;

            // Update player name in center
            if (Player.m_localPlayer != null)
            {
                m_centerPlayerName.text = Player.m_localPlayer.GetPlayerName();
            }

            m_menuRoot.SetActive(true);
            m_canvasGroup.alpha = 1.0f;

            RefreshCheckmarks();
            UpdateHover(m_hoveredIndex);
        }

        public void Hide()
        {
            m_isOpen = false;
            if (m_menuRoot != null)
            {
                m_menuRoot.SetActive(false);
            }
        }

        public void ConfirmSelection()
        {
            if (m_hoveredIndex >= 0 && m_hoveredIndex < m_sectors.Count)
            {
                PlayerColorDefinition chosen = m_sectors[m_hoveredIndex].Definition;
                ModConfig.ActiveColorIndex.Value = chosen.Id;
                ModConfig.CustomPlayerColor.Value = chosen.Color;

                // Сохраняем в файл конфигурации
                PingMakerPlugin.Instance.Config.Save();

                // Оповещение в игре
                if (Player.m_localPlayer != null)
                {
                    string hexColor = ColorUtility.ToHtmlStringRGB(chosen.Color);
                    Player.m_localPlayer.Message(
                        MessageHud.MessageType.TopLeft,
                        $"<color=#{hexColor}>[PingMaker]</color> Ваш личный цвет пинга: <color=#{hexColor}><b>{chosen.Name}</b></color>"
                    );
                }

                PingMakerPlugin.Log.LogInfo($"[PingMaker] Выбран личный цвет: {chosen.Name} ({chosen.Color})");
            }

            Hide();
        }

        public Color GetActiveChosenColor()
        {
            return PlayerColors.GetActiveColor();
        }

        private void RefreshCheckmarks()
        {
            int activeId = ModConfig.ActiveColorIndex.Value;
            for (int i = 0; i < m_sectors.Count; i++)
            {
                bool isActive = (m_sectors[i].Index == activeId);
                m_sectors[i].CheckmarkImage.gameObject.SetActive(isActive);
            }
        }

        private void Update()
        {
            if (!m_isOpen) return;

            Vector2 mousePos = Input.mousePosition;
            Vector2 delta = mousePos - m_centerPos;

            if (delta.magnitude > 22f)
            {
                float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
                float clockwise = (90f - angle + 360f) % 360f;
                float step = 360f / m_sectors.Count;

                int sector = Mathf.FloorToInt((clockwise + step * 0.5f) / step) % m_sectors.Count;
                if (sector != m_hoveredIndex)
                {
                    UpdateHover(sector);
                }
            }
        }

        private void UpdateHover(int index)
        {
            m_hoveredIndex = index;

            for (int i = 0; i < m_sectors.Count; i++)
            {
                ColorSectorItem item = m_sectors[i];
                bool isHovered = (i == index);

                if (isHovered)
                {
                    item.Rect.localScale = Vector3.one * 1.35f;
                }
                else
                {
                    item.Rect.localScale = Vector3.one;
                }
            }

            if (index >= 0 && index < m_sectors.Count)
            {
                ColorSectorItem selected = m_sectors[index];
                m_centerColorLabel.text = selected.Definition.Name;
                m_centerColorLabel.color = selected.Definition.Color;
                m_centerPlayerName.color = selected.Definition.Color;
            }
        }
    }
}
