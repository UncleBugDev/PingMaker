using System.Collections.Generic;
using UnityEngine;

namespace PingMaker.Visuals
{
    /// <summary>
    /// Компонент подсветки силуэта и контура существа при установке пинга.
    /// Использует MaterialMan и MaterialPropertyBlock движка Valheim без выделения дубликатов материалов.
    /// </summary>
    public class CreatureHighlighter : MonoBehaviour
    {
        private Character m_character = null!;
        private Color m_baseColor;
        private float m_duration;
        private float m_elapsed;
        private bool m_isInitialized;

        private static readonly int s_emissionColor = ShaderProps._EmissionColor;
        private static readonly int s_mainColor = ShaderProps._Color;

        public static void ApplyHighlight(Character character, Color color, float duration)
        {
            if (character == null || character.gameObject == null) return;

            CreatureHighlighter existing = character.GetComponent<CreatureHighlighter>();
            if (existing != null)
            {
                // Если существо уже подсвечено, обновляем таймер и цвет
                existing.Refresh(color, duration);
                return;
            }

            CreatureHighlighter highlighter = character.gameObject.AddComponent<CreatureHighlighter>();
            highlighter.Initialize(character, color, duration);
        }

        private void Initialize(Character character, Color color, float duration)
        {
            m_character = character;
            m_baseColor = color;
            m_duration = duration > 0f ? duration : 18f;
            m_elapsed = 0f;
            m_isInitialized = true;

            ApplyMaterialProperties(m_baseColor * 2.0f);
        }

        public void Refresh(Color newColor, float duration)
        {
            m_baseColor = newColor;
            m_duration = duration > 0f ? duration : 18f;
            m_elapsed = 0f;

            ApplyMaterialProperties(m_baseColor * 2.0f);
        }

        private void Update()
        {
            if (!m_isInitialized || m_character == null)
            {
                Destroy(this);
                return;
            }

            // Автоматическое снятие подсветки при гибели существа
            if (m_character.IsDead())
            {
                Destroy(this);
                return;
            }

            m_elapsed += Time.deltaTime;
            if (m_elapsed >= m_duration)
            {
                Destroy(this);
                return;
            }

            // Плавное затухание в последние 2 секунды
            float alpha = 1.0f;
            float remaining = m_duration - m_elapsed;
            if (remaining < 2.0f)
            {
                alpha = Mathf.Clamp01(remaining / 2.0f);
            }

            // Пульсирующий синусоидальный эффект свечения
            float pulse = 1.0f + 0.35f * Mathf.Sin(Time.time * 6.0f);
            Color pulsedColor = m_baseColor * (pulse * 2.2f * alpha);

            ApplyMaterialProperties(pulsedColor);
        }

        private void ApplyMaterialProperties(Color emission)
        {
            if (m_character == null || m_character.gameObject == null) return;

            if (MaterialMan.instance != null)
            {
                MaterialMan.instance.SetValue(m_character.gameObject, s_emissionColor, emission);
            }
        }

        private void OnDestroy()
        {
            if (m_character != null && m_character.gameObject != null && MaterialMan.instance != null)
            {
                MaterialMan.instance.ResetValue(m_character.gameObject, s_emissionColor);
            }
        }
    }
}
