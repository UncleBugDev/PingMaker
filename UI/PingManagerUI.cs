using System.Collections.Generic;
using PingMaker.Configuration;
using PingMaker.Core;
using PingMaker.Network;
using PingMaker.Visuals;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.UI
{
    /// <summary>
    /// Главный менеджер UI маркеров пинга на полноэкранном холсте.
    /// </summary>
    public class PingManagerUI : MonoBehaviour
    {
        public static PingManagerUI? Instance { get; private set; }

        private Canvas m_canvas = null!;
        private CanvasScaler m_scaler = null!;
        private readonly List<PingMarker3D> m_activeMarkers = new List<PingMarker3D>();

        public static void EnsureExists()
        {
            if (Instance != null) return;

            GameObject uiRoot = new GameObject("PingMaker_UI_Root");
            DontDestroyOnLoad(uiRoot);
            Instance = uiRoot.AddComponent<PingManagerUI>();
        }

        private void Awake()
        {
            Instance = this;

            m_canvas = gameObject.AddComponent<Canvas>();
            m_canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            m_canvas.sortingOrder = 450; // Visible above 3D world, below console and main menus

            m_scaler = gameObject.AddComponent<CanvasScaler>();
            m_scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            m_scaler.referenceResolution = new Vector2(1920f, 1080f);
            m_scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();
            TargetLockonReticle.EnsureExists(transform);
        }

        public void SpawnPing(PingPackageData data)
        {
            // Limit active pings per player
            CleanupPlayerPings(data.SenderName, ModConfig.MaxActivePingsPerPlayer.Value - 1);

            GameObject markerObj = new GameObject($"PingMarker_{data.SenderName}_{Time.time}");
            markerObj.transform.SetParent(transform, false);

            Transform? trackedTransform = null;
            Character? targetCharacter = null;

            // If creature ZDOID was provided, find its live head transform
            if (data.TargetCreatureZDOID != ZDOID.None && ZNetScene.instance != null)
            {
                GameObject creatureGO = ZNetScene.instance.FindInstance(data.TargetCreatureZDOID);
                if (creatureGO != null)
                {
                    targetCharacter = creatureGO.GetComponent<Character>();
                    if (targetCharacter != null)
                    {
                        trackedTransform = targetCharacter.transform;
                        // Apply creature highlight
                        CreatureHighlighter.ApplyHighlight(targetCharacter, data.PingColor, ModConfig.CreatureHighlightDuration.Value);
                    }
                }
            }

            // Create temporary minimap pin
            Minimap.PinData? mapPin = null;
            if (ModConfig.SyncToMinimap.Value && Minimap.instance != null)
            {
                try
                {
                    string pinName = !string.IsNullOrEmpty(data.TargetName)
                        ? $"{data.SenderName} ({data.TargetName})"
                        : $"{data.SenderName}: Пинг";

                    mapPin = Minimap.instance.AddPin(
                        data.WorldPosition,
                        Minimap.PinType.Ping,
                        pinName,
                        save: false,
                        isChecked: false
                    );

                    if (mapPin != null)
                    {
                        mapPin.m_animate = true;
                        mapPin.m_doubleSize = true;
                        Sprite whiteSprite = ProceduralIcons.GetWhiteMapPingSprite();
                        mapPin.m_icon = whiteSprite;
                        if (mapPin.m_iconElement != null)
                        {
                            mapPin.m_iconElement.sprite = whiteSprite;
                            mapPin.m_iconElement.color = data.PingColor;
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    PingMakerPlugin.Log.LogWarning($"[PingMaker] Ошибка создания метки на карте: {ex.Message}");
                }
            }

            PingMarker3D marker = markerObj.AddComponent<PingMarker3D>();
            marker.Setup(data, trackedTransform, mapPin, targetCharacter);
            m_activeMarkers.Add(marker);

            PlayPingAudio();
        }

        public void UpdateMapPinsColor()
        {
            Sprite whiteSprite = ProceduralIcons.GetWhiteMapPingSprite();
            for (int i = 0; i < m_activeMarkers.Count; i++)
            {
                PingMarker3D marker = m_activeMarkers[i];
                if (marker != null && marker.MapPin != null)
                {
                    if (marker.MapPin.m_icon != whiteSprite)
                    {
                        marker.MapPin.m_icon = whiteSprite;
                    }

                    if (marker.MapPin.m_iconElement != null)
                    {
                        if (marker.MapPin.m_iconElement.sprite != whiteSprite)
                        {
                            marker.MapPin.m_iconElement.sprite = whiteSprite;
                        }
                        marker.MapPin.m_iconElement.color = marker.Data.PingColor;
                    }

                    if (marker.MapPin.m_NamePinData?.PinNameText != null)
                    {
                        marker.MapPin.m_NamePinData.PinNameText.color = marker.Data.PingColor;
                    }
                }
            }
        }

        private void CleanupPlayerPings(string senderName, int maxAllowed)
        {
            if (maxAllowed < 0) maxAllowed = 0;

            List<PingMarker3D> playerPings = new List<PingMarker3D>();
            for (int i = m_activeMarkers.Count - 1; i >= 0; i--)
            {
                if (m_activeMarkers[i] == null)
                {
                    m_activeMarkers.RemoveAt(i);
                    continue;
                }

                if (m_activeMarkers[i].SenderName == senderName)
                {
                    playerPings.Add(m_activeMarkers[i]);
                }
            }

            while (playerPings.Count > maxAllowed)
            {
                PingMarker3D oldest = playerPings[playerPings.Count - 1];
                playerPings.RemoveAt(playerPings.Count - 1);
                m_activeMarkers.Remove(oldest);
                if (oldest != null)
                {
                    Destroy(oldest.gameObject);
                }
            }
        }

        private static AudioClip? s_pingAudioClip;

        private void PlayPingAudio()
        {
            if (ModConfig.AudioVolume.Value <= 0.01f) return;

            try
            {
                if (s_pingAudioClip == null)
                {
                    s_pingAudioClip = CreateProceduralPingClip();
                }

                Camera cam = Utils.GetMainCamera();
                Vector3 pos = cam != null ? cam.transform.position : Vector3.zero;
                AudioSource.PlayClipAtPoint(s_pingAudioClip, pos, ModConfig.AudioVolume.Value);
            }
            catch
            {
                // Игнорируем исключения звукового устройства
            }
        }

        private static AudioClip CreateProceduralPingClip()
        {
            const int sampleRate = 44100;
            const float duration = 0.12f;
            int sampleCount = (int)(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float t = (float)i / sampleRate;
                float decay = Mathf.Exp(-t * 22f);
                float wave = (Mathf.Sin(2f * Mathf.PI * 987.77f * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * 1318.5f * t) * 0.4f);
                samples[i] = wave * decay;
            }

            AudioClip clip = AudioClip.Create("PingMaker_Chime", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
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
