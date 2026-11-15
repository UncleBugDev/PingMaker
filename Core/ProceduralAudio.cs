using System;
using System.Collections.Generic;
using UnityEngine;

namespace PingMaker.Core
{
    /// <summary>
    /// Процедурная генерация звуковых эффектов пингов в оперативной памяти (без внешних аудио-ассетов).
    /// Поддерживает пресеты звучания, индивидуальную регулировку громкости и защиту от звукового спама.
    /// </summary>
    public static class ProceduralAudio
    {
        public const int PresetCount = 5;

        private static readonly Dictionary<int, AudioClip> s_clipCache = new Dictionary<int, AudioClip>();
        private static float s_lastPlayTime;

        public static string GetPresetName(int presetIndex)
        {
            switch (presetIndex)
            {
                case 0: return "Мягкий щелчок (Soft Click)";
                case 1: return "Кристальный звон (Classic Chime)";
                case 2: return "Рог викингов (Nordic Horn)";
                case 3: return "Деревянный стук (Wood Knock)";
                case 4: return "Без звука (Mute)";
                default: return "По умолчанию";
            }
        }

        public static AudioClip? GetClip(int presetIndex)
        {
            if (presetIndex == 4) return null; // Mute

            if (s_clipCache.TryGetValue(presetIndex, out AudioClip cached) && cached != null)
            {
                return cached;
            }

            AudioClip generated;
            switch (presetIndex)
            {
                case 0:
                    generated = GenerateSoftClick();
                    break;
                case 1:
                    generated = GenerateClassicChime();
                    break;
                case 2:
                    generated = GenerateNordicHorn();
                    break;
                case 3:
                    generated = GenerateWoodKnock();
                    break;
                default:
                    generated = GenerateSoftClick();
                    break;
            }

            s_clipCache[presetIndex] = generated;
            return generated;
        }

        public static void PlayPing(int presetIndex, float volume, Vector3? worldPos = null)
        {
            if (presetIndex == 4 || volume <= 0.01f) return;

            // Защита от аудио-спама (cooldown 90мс)
            if (Time.unscaledTime - s_lastPlayTime < 0.09f) return;
            s_lastPlayTime = Time.unscaledTime;

            PlayDirect(presetIndex, volume, worldPos);
        }

        public static void PlayPreview(int presetIndex, float volume)
        {
            if (presetIndex == 4 || volume <= 0.01f) return;
            PlayDirect(presetIndex, volume, null);
        }

        private static void PlayDirect(int presetIndex, float volume, Vector3? worldPos)
        {
            try
            {
                AudioClip? clip = GetClip(presetIndex);
                if (clip == null) return;

                Camera cam = Utils.GetMainCamera();
                Vector3 soundPos = cam != null ? cam.transform.position : (worldPos ?? Vector3.zero);

                // Воспроизведение 2D звука у камеры игрока
                AudioSource.PlayClipAtPoint(clip, soundPos, Mathf.Clamp01(volume));
            }
            catch
            {
                // Игнорируем ошибки отсутствия аудио-устройств
            }
        }

        /// <summary>
        /// Пресет 0: Мягкий тактический щелчок (45мс).
        /// Идеален для частых пингов, частотный спад 420 Гц -> 260 Гц, не утомляет уши.
        /// </summary>
        private static AudioClip GenerateSoftClick()
        {
            const int sampleRate = 44100;
            const float duration = 0.048f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Плавная атака (2мс) и мягкий экспоненциальный спад
                float attack = Mathf.Clamp01(t / 0.0025f);
                float decay = Mathf.Exp(-progress * 9f);
                float envelope = attack * decay;

                // Мягкое скольжение частоты вниз: 430 Гц -> 260 Гц
                float freq = Mathf.Lerp(430f, 260f, progress * progress);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t);

                // Добавление мягкого призвука второй гармоники
                float harmonic = Mathf.Sin(2f * Mathf.PI * (freq * 1.5f) * t) * 0.25f;

                samples[i] = (wave * 0.75f + harmonic) * envelope * 0.85f;
            }

            AudioClip clip = AudioClip.Create("PingMaker_SoftClick", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Пресет 1: Классический кристальный звон (120мс).
        /// Двухтоновый аккорд 988 Гц + 1318 Гц с ярким чистым затуханием.
        /// </summary>
        private static AudioClip GenerateClassicChime()
        {
            const int sampleRate = 44100;
            const float duration = 0.12f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float attack = Mathf.Clamp01(t / 0.003f);
                float decay = Mathf.Exp(-t * 22f);
                float envelope = attack * decay;

                float wave1 = Mathf.Sin(2f * Mathf.PI * 987.77f * t) * 0.60f;
                float wave2 = Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.40f;

                samples[i] = (wave1 + wave2) * envelope * 0.75f;
            }

            AudioClip clip = AudioClip.Create("PingMaker_ClassicChime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Пресет 2: Низкий рог викингов (220мс).
        /// Басовый обертональный рог (165 Гц + 330 Гц + 495 Гц) с мягкой атакой и гулким телом.
        /// </summary>
        private static AudioClip GenerateNordicHorn()
        {
            const int sampleRate = 44100;
            const float duration = 0.22f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                // Нарастающая атака рога (18мс)
                float attack = Mathf.Clamp01(t / 0.018f);
                float decay = Mathf.Exp(-progress * 4.5f);
                float envelope = attack * decay;

                // Основной тон 165 Гц (нота E3) + гармоники
                float f1 = Mathf.Sin(2f * Mathf.PI * 164.8f * t) * 0.55f;
                float f2 = Mathf.Sin(2f * Mathf.PI * 329.6f * t) * 0.30f;
                float f3 = Mathf.Sin(2f * Mathf.PI * 494.4f * t) * 0.15f;

                samples[i] = (f1 + f2 + f3) * envelope * 0.90f;
            }

            AudioClip clip = AudioClip.Create("PingMaker_NordicHorn", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        /// <summary>
        /// Пресет 3: Деревянный глухой стук (60мс).
        /// Перкуссионный стук деревянного щита/топора (240 Гц со спадом до 120 Гц).
        /// </summary>
        private static AudioClip GenerateWoodKnock()
        {
            const int sampleRate = 44100;
            const float duration = 0.065f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float progress = t / duration;

                float attack = Mathf.Clamp01(t / 0.002f);
                float decay = Mathf.Exp(-progress * 16f);
                float envelope = attack * decay;

                float freq = Mathf.Lerp(260f, 130f, progress);
                float body = Mathf.Sin(2f * Mathf.PI * freq * t);
                float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * t) * 0.4f;

                samples[i] = (body * 0.7f + sub) * envelope * 0.95f;
            }

            AudioClip clip = AudioClip.Create("PingMaker_WoodKnock", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
