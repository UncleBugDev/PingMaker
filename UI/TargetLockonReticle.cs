using PingMaker.Configuration;
using PingMaker.Core;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.UI
{
    /// <summary>
    /// Тактический прицел захвата цели (Lock-on Reticle).
    /// Отображается на экране при наведении перекрестия на монстра или зверя:
    /// кольцо вокруг существа в личном цвете игрока, анимированная указующая стрелочка и имя цели.
    /// </summary>
    public class TargetLockonReticle : MonoBehaviour
    {
        public static TargetLockonReticle? Instance { get; private set; }

        private RectTransform m_rect = null!;
        private CanvasGroup m_canvasGroup = null!;

        // UI elements
        private RectTransform m_circleRect = null!;
        private Image m_circleImage = null!;

        private RectTransform m_arrowRect = null!;
        private Image m_arrowImage = null!;

        private RectTransform m_textRect = null!;
        private Text m_nameText = null!;

        private Character? m_currentTarget;
        private float m_targetAlpha;
        private float m_currentAlpha;

        public static void EnsureExists(Transform parentCanvas)
        {
            if (Instance != null) return;

            GameObject reticleObj = new GameObject("PingMaker_LockonReticle");
            reticleObj.transform.SetParent(parentCanvas, false);
            Instance = reticleObj.AddComponent<TargetLockonReticle>();
        }

        private void Awake()
        {
            Instance = this;
            BuildUI();
        }

        private void BuildUI()
        {
            m_rect = gameObject.AddComponent<RectTransform>();
            m_rect.sizeDelta = new Vector2(120f, 120f);
            m_rect.pivot = new Vector2(0.5f, 0.5f);

            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.blocksRaycasts = false;
            m_canvasGroup.interactable = false;

            // 1. Кольцо захвата цели
            GameObject circleObj = new GameObject("Circle", typeof(RectTransform), typeof(Image));
            circleObj.transform.SetParent(transform, false);
            m_circleRect = circleObj.GetComponent<RectTransform>();
            m_circleRect.sizeDelta = new Vector2(74f, 74f);
            m_circleRect.anchoredPosition = Vector2.zero;

            m_circleImage = circleObj.GetComponent<Image>();
            m_circleImage.sprite = ProceduralIcons.GetLockonCircleSprite();
            m_circleImage.raycastTarget = false;

            // 2. Указующая стрелочка над кольцом
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(transform, false);
            m_arrowRect = arrowObj.GetComponent<RectTransform>();
            m_arrowRect.sizeDelta = new Vector2(28f, 28f);
            m_arrowRect.anchoredPosition = new Vector2(0f, 48f);

            m_arrowImage = arrowObj.GetComponent<Image>();
            m_arrowImage.sprite = ProceduralIcons.GetLockonArrowSprite();
            m_arrowImage.raycastTarget = false;

            // 3. Название существа и звёзды
            Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

            GameObject textObj = new GameObject("TargetName", typeof(RectTransform), typeof(Text), typeof(Outline));
            textObj.transform.SetParent(transform, false);
            m_textRect = textObj.GetComponent<RectTransform>();
            m_textRect.sizeDelta = new Vector2(220f, 22f);
            m_textRect.anchoredPosition = new Vector2(0f, 72f);

            m_nameText = textObj.GetComponent<Text>();
            m_nameText.font = font;
            m_nameText.fontSize = 13;
            m_nameText.fontStyle = FontStyle.Bold;
            m_nameText.alignment = TextAnchor.MiddleCenter;
            m_nameText.color = Color.white;
            m_nameText.raycastTarget = false;

            Outline outline = textObj.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.95f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        public void SetTarget(Character? character)
        {
            if (character == null || character.IsDead() || character.gameObject == null)
            {
                m_currentTarget = null;
                m_targetAlpha = 0f;
                return;
            }

            m_currentTarget = character;
            m_targetAlpha = 1f;

            string name = character.GetHoverName();
            int level = character.GetLevel();
            string star = level > 1 ? $" (★{level - 1})" : "";
            m_nameText.text = $"{name}{star}";
        }

        private void Update()
        {
            // Плавное появление / исчезновение
            m_currentAlpha = Mathf.MoveTowards(m_currentAlpha, m_targetAlpha, Time.deltaTime * 8.0f);
            m_canvasGroup.alpha = m_currentAlpha;

            if (m_currentAlpha <= 0.001f || m_currentTarget == null)
            {
                return;
            }

            // Проверка жизнеспособности цели
            if (m_currentTarget.IsDead() || m_currentTarget.gameObject == null)
            {
                m_currentTarget = null;
                m_targetAlpha = 0f;
                return;
            }

            Camera cam = Utils.GetMainCamera();
            if (cam == null) return;

            Vector3 targetPos = m_currentTarget.GetCenterPoint();
            Vector3 screenPos = cam.WorldToScreenPoint(targetPos);

            // Если существо за спиной камеры, скрываем прицел
            if (screenPos.z <= 0.1f)
            {
                m_canvasGroup.alpha = 0f;
                return;
            }

            m_rect.position = new Vector2(screenPos.x, screenPos.y);

            // Окрашивание в активный личный цвет игрока
            Color personalColor = PlayerColors.GetActiveColor();
            m_circleImage.color = personalColor;
            m_arrowImage.color = personalColor;

            // Плавное вращение кольца
            m_circleRect.Rotate(0f, 0f, -25f * Time.deltaTime);

            // Плавная вертикальная анимация покачивания стрелочки (bobbing)
            float bob = Mathf.Sin(Time.time * 8.0f) * 4.0f;
            m_arrowRect.anchoredPosition = new Vector2(0f, 48f + bob);
        }
    }
}
