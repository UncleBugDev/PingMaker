using System;
using PingMaker.Configuration;
using PingMaker.Core;
using PingMaker.Network;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.Visuals
{
    /// <summary>
    /// 3D-маркер пинга на полноэкранном холсте (ScreenSpaceOverlay).
    /// Поддерживает расчет расстояния, ник игрока, прилипание к краю экрана (edge clamping)
    /// и стрелку-указатель, если цель находится за пределами обзора.
    /// </summary>
    public class PingMarker3D : MonoBehaviour
    {
        private PingPackageData m_data;
        private Transform? m_trackedTransform;
        private Vector3 m_staticWorldPos;
        private float m_duration;
        private float m_elapsed;

        // UI Components
        private RectTransform m_rect = null!;
        private CanvasGroup m_canvasGroup = null!;
        private Image m_iconImage = null!;
        private Image m_arrowImage = null!;
        private RectTransform m_arrowRect = null!;
        private Text m_playerText = null!;
        private Text m_distanceText = null!;
        private Text m_titleText = null!;

        public PingPackageData Data => m_data;
        public string SenderName => m_data.SenderName;

        public void Setup(PingPackageData data, Transform? trackedTarget = null)
        {
            m_data = data;
            m_trackedTransform = trackedTarget;
            m_staticWorldPos = data.WorldPosition;
            m_duration = data.Duration > 0f ? data.Duration : 20f;
            m_elapsed = 0f;

            BuildUI();
        }

        private void BuildUI()
        {
            m_rect = gameObject.AddComponent<RectTransform>();
            m_rect.sizeDelta = new Vector2(100f, 100f);
            m_rect.pivot = new Vector2(0.5f, 0.5f);

            m_canvasGroup = gameObject.AddComponent<CanvasGroup>();

            // 1. Icon Object
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(transform, false);
            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.sizeDelta = new Vector2(42f, 42f);
            iconRect.anchoredPosition = Vector2.zero;

            m_iconImage = iconObj.GetComponent<Image>();
            m_iconImage.sprite = ProceduralIcons.GetPingSprite(m_data.Category);
            m_iconImage.color = m_data.PingColor;

            // 2. Directional Arrow Object (for off-screen indication)
            GameObject arrowObj = new GameObject("Arrow", typeof(RectTransform), typeof(Image));
            arrowObj.transform.SetParent(transform, false);
            m_arrowRect = arrowObj.GetComponent<RectTransform>();
            m_arrowRect.sizeDelta = new Vector2(24f, 24f);
            m_arrowRect.anchoredPosition = new Vector2(0f, 32f);

            m_arrowImage = arrowObj.GetComponent<Image>();
            m_arrowImage.sprite = ProceduralIcons.GetPointerArrowSprite();
            m_arrowImage.color = m_data.PingColor;
            arrowObj.SetActive(false);

            // Font reference from Valheim or fallback
            Font font = Font.CreateDynamicFontFromOSFont("Arial", 14);

            // 3. Player Nickname Text
            if (ModConfig.ShowPlayerName.Value && !string.IsNullOrEmpty(m_data.SenderName))
            {
                GameObject playerObj = new GameObject("PlayerText", typeof(RectTransform), typeof(Text), typeof(Outline));
                playerObj.transform.SetParent(transform, false);
                RectTransform pRect = playerObj.GetComponent<RectTransform>();
                pRect.sizeDelta = new Vector2(180f, 22f);
                pRect.anchoredPosition = new Vector2(0f, 30f);

                m_playerText = playerObj.GetComponent<Text>();
                m_playerText.font = font;
                m_playerText.fontSize = 13;
                m_playerText.fontStyle = FontStyle.Bold;
                m_playerText.alignment = TextAnchor.MiddleCenter;
                m_playerText.text = m_data.SenderName;
                m_playerText.color = m_data.PingColor;

                Outline outline = playerObj.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // 4. Target Creature Title Text (if pinged a monster)
            if (!string.IsNullOrEmpty(m_data.TargetName))
            {
                GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(Text), typeof(Outline));
                titleObj.transform.SetParent(transform, false);
                RectTransform tRect = titleObj.GetComponent<RectTransform>();
                tRect.sizeDelta = new Vector2(200f, 22f);
                tRect.anchoredPosition = new Vector2(0f, 46f);

                m_titleText = titleObj.GetComponent<Text>();
                m_titleText.font = font;
                m_titleText.fontSize = 12;
                m_titleText.fontStyle = FontStyle.Bold;
                m_titleText.alignment = TextAnchor.MiddleCenter;
                string star = m_data.TargetLevel > 1 ? $" (★{m_data.TargetLevel - 1})" : "";
                m_titleText.text = $"{m_data.TargetName}{star}";
                m_titleText.color = Color.white;

                Outline outline = titleObj.GetComponent<Outline>();
                outline.effectColor = new Color(0.8f, 0.1f, 0.1f, 0.95f);
                outline.effectDistance = new Vector2(1f, -1f);
            }

            // 5. Distance Text
            if (ModConfig.ShowDistance.Value)
            {
                GameObject distObj = new GameObject("DistanceText", typeof(RectTransform), typeof(Text), typeof(Outline));
                distObj.transform.SetParent(transform, false);
                RectTransform dRect = distObj.GetComponent<RectTransform>();
                dRect.sizeDelta = new Vector2(140f, 20f);
                dRect.anchoredPosition = new Vector2(0f, -28f);

                m_distanceText = distObj.GetComponent<Text>();
                m_distanceText.font = font;
                m_distanceText.fontSize = 12;
                m_distanceText.fontStyle = FontStyle.Bold;
                m_distanceText.alignment = TextAnchor.MiddleCenter;
                m_distanceText.text = "[0m]";
                m_distanceText.color = Color.white;

                Outline outline = distObj.GetComponent<Outline>();
                outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
                outline.effectDistance = new Vector2(1f, -1f);
            }
        }

        private void Update()
        {
            m_elapsed += Time.deltaTime;
            if (m_elapsed >= m_duration)
            {
                Destroy(gameObject);
                return;
            }

            // Fade out in last 2 seconds
            float remaining = m_duration - m_elapsed;
            if (remaining < 2.0f && m_canvasGroup != null)
            {
                m_canvasGroup.alpha = Mathf.Clamp01(remaining / 2.0f);
            }

            Camera cam = Utils.GetMainCamera();
            if (cam == null) return;

            Vector3 worldPos = m_trackedTransform != null ? m_trackedTransform.position : m_staticWorldPos;

            // Distance Calculation
            float distance = Vector3.Distance(cam.transform.position, worldPos);
            if (m_distanceText != null)
            {
                m_distanceText.text = $"[{Mathf.RoundToInt(distance)}m]";
            }

            // Screen Projection
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            bool isBehind = screenPos.z <= 0f;

            if (isBehind)
            {
                screenPos.x = -screenPos.x;
                screenPos.y = -screenPos.y;
            }

            float screenW = Screen.width;
            float screenH = Screen.height;
            float margin = 50f * ModConfig.MarkerScale.Value;

            bool isOffscreen = isBehind ||
                              screenPos.x < margin || screenPos.x > (screenW - margin) ||
                              screenPos.y < margin || screenPos.y > (screenH - margin);

            Vector2 clampedPos;

            if (isOffscreen && ModConfig.ShowOffscreenArrow.Value)
            {
                Vector2 screenCenter = new Vector2(screenW * 0.5f, screenH * 0.5f);
                Vector2 fromCenter = new Vector2(screenPos.x, screenPos.y) - screenCenter;

                if (fromCenter.sqrMagnitude < 0.001f)
                {
                    fromCenter = Vector2.up;
                }
                fromCenter.Normalize();

                float maxX = (screenW * 0.5f) - margin;
                float maxY = (screenH * 0.5f) - margin;

                float factorX = Mathf.Abs(maxX / fromCenter.x);
                float factorY = Mathf.Abs(maxY / fromCenter.y);
                float factor = Mathf.Min(factorX, factorY);

                clampedPos = screenCenter + fromCenter * factor;

                // Show and point arrow
                if (m_arrowImage != null && m_arrowRect != null)
                {
                    m_arrowImage.gameObject.SetActive(true);
                    float angle = Mathf.Atan2(fromCenter.y, fromCenter.x) * Mathf.Rad2Deg - 90f;
                    m_arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
                    m_arrowRect.anchoredPosition = fromCenter * 30f;
                }

                // Hide detailed labels when clamped to edge to avoid screen clutter
                if (m_playerText != null) m_playerText.gameObject.SetActive(false);
                if (m_titleText != null) m_titleText.gameObject.SetActive(false);
            }
            else
            {
                clampedPos = new Vector2(
                    Mathf.Clamp(screenPos.x, margin, screenW - margin),
                    Mathf.Clamp(screenPos.y, margin, screenH - margin)
                );

                if (m_arrowImage != null)
                {
                    m_arrowImage.gameObject.SetActive(false);
                }

                if (m_playerText != null) m_playerText.gameObject.SetActive(true);
                if (m_titleText != null) m_titleText.gameObject.SetActive(true);
            }

            m_rect.position = clampedPos;

            // Subtle pulsing scale
            float pulse = 1.0f + 0.08f * Mathf.Sin(Time.time * 5.0f);
            float scale = ModConfig.MarkerScale.Value * pulse;
            m_rect.localScale = new Vector3(scale, scale, 1.0f);
        }
    }
}
