using System;
using System.Collections.Generic;
using HarmonyLib;
using PingMaker.Configuration;
using PingMaker.Core;
using PingMaker.Network;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.Visuals
{
    /// <summary>
    /// Окрашивает маркеры и стрелки направления игроков на карте и миникарте
    /// в их персональный выбранный цвет (вместо однообразных красных иконок).
    /// </summary>
    public static class PlayerMapTracker
    {
        private static readonly Dictionary<string, Vector3> s_lastPlayerPositions = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        private static readonly AccessTools.FieldRef<Minimap, List<Minimap.PinData>> s_playerPinsRef = AccessTools.FieldRefAccess<Minimap, List<Minimap.PinData>>("m_playerPins");

        public static void UpdateMinimap(Minimap map)
        {
            if (map == null || !ModConfig.ColorizeMapPlayers.Value) return;

            // 1. Окрашивание маркера локального игрока (направление взгляда/стрелка на миникарте и большой карте)
            UpdateLocalPlayer(map);

            // 2. Окрашивание меток и стрелок направления остальных игроков
            UpdateOtherPlayers(map);
        }

        public static void UpdateLocalPlayer(Minimap map)
        {
            if (map == null || !ModConfig.ColorizeMapPlayers.Value) return;

            Color localColor = PlayerColors.GetActiveColor();

            if (map.m_smallMarker != null)
            {
                Image[] smallImgs = map.m_smallMarker.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < smallImgs.Length; i++)
                {
                    smallImgs[i].color = localColor;
                }
            }

            if (map.m_largeMarker != null)
            {
                Image[] largeImgs = map.m_largeMarker.GetComponentsInChildren<Image>(true);
                for (int i = 0; i < largeImgs.Length; i++)
                {
                    largeImgs[i].color = localColor;
                }
            }
        }

        private static void UpdateOtherPlayers(Minimap map)
        {
            List<Minimap.PinData> playerPins;
            try
            {
                playerPins = s_playerPinsRef(map);
            }
            catch
            {
                return;
            }

            if (playerPins == null || playerPins.Count == 0) return;

            Sprite arrowSprite = ProceduralIcons.GetPlayerArrowMarkerSprite();
            List<Player> allPlayers = Player.GetAllPlayers();

            for (int i = 0; i < playerPins.Count; i++)
            {
                Minimap.PinData pin = playerPins[i];
                if (pin == null || string.IsNullOrEmpty(pin.m_name)) continue;

                Color playerColor = NetworkSync.GetPlayerColor(pin.m_name);

                // Заменяем иконку на четкую направленную стрелку с окантовкой
                if (pin.m_iconElement != null)
                {
                    if (pin.m_iconElement.sprite != arrowSprite)
                    {
                        pin.m_iconElement.sprite = arrowSprite;
                    }
                    pin.m_iconElement.color = playerColor;
                }

                // Окрашиваем текст имени игрока под меткой
                if (pin.m_NamePinData?.PinNameText != null)
                {
                    pin.m_NamePinData.PinNameText.color = playerColor;
                }

                // Вращение стрелки по направлению взгляда (или движения)
                if (pin.m_uiElement != null)
                {
                    Player? found = null;
                    if (allPlayers != null)
                    {
                        for (int p = 0; p < allPlayers.Count; p++)
                        {
                            if (allPlayers[p] != null && allPlayers[p].GetPlayerName() == pin.m_name)
                            {
                                found = allPlayers[p];
                                break;
                            }
                        }
                    }

                    if (found != null)
                    {
                        float heading = found.transform.eulerAngles.y;
                        pin.m_uiElement.rotation = Quaternion.Euler(0f, 0f, -heading);
                    }
                    else if (s_lastPlayerPositions.TryGetValue(pin.m_name, out Vector3 lastPos))
                    {
                        Vector3 delta = pin.m_pos - lastPos;
                        if (delta.sqrMagnitude > 0.04f)
                        {
                            float heading = Mathf.Atan2(delta.x, delta.z) * Mathf.Rad2Deg;
                            pin.m_uiElement.rotation = Quaternion.Euler(0f, 0f, -heading);
                        }
                    }

                    s_lastPlayerPositions[pin.m_name] = pin.m_pos;
                }
            }
        }
    }
}
