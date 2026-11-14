using System.Collections;
using System.Reflection;
using HarmonyLib;
using PingMaker.Network;
using TMPro;
using UnityEngine;

namespace PingMaker.Visuals
{
    /// <summary>
    /// Окрашивает имена игроков в их выбранный личный цвет викинга:
    /// 1. 3D-плавающие имена над головами игроков (EnemyHud).
    /// 2. Текст подсказки по центру экрана при наведении перекрестия на игрока (Hud.m_hoverName).
    /// </summary>
    public static class PlayerNameColorizer
    {
        [HarmonyPatch(typeof(EnemyHud), "UpdateHuds")]
        public static class EnemyHud_UpdateHuds_Patch
        {
            private static readonly FieldInfo s_hudsField = AccessTools.Field(typeof(EnemyHud), "m_huds");
            private static readonly FieldInfo s_nameField = AccessTools.Field(AccessTools.Inner(typeof(EnemyHud), "HudData"), "m_name");

            public static void Postfix(EnemyHud __instance)
            {
                if (s_hudsField == null || s_nameField == null || __instance == null) return;

                var huds = s_hudsField.GetValue(__instance) as IDictionary;
                if (huds == null) return;

                foreach (DictionaryEntry entry in huds)
                {
                    if (entry.Key is Character ch && ch.IsPlayer())
                    {
                        object hudData = entry.Value;
                        if (hudData != null)
                        {
                            var tmp = s_nameField.GetValue(hudData) as TextMeshProUGUI;
                            if (tmp != null)
                            {
                                string playerName = ch.GetHoverName();
                                Color playerColor = NetworkSync.GetPlayerColor(playerName);
                                tmp.color = playerColor;
                            }
                        }
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
        public static class Hud_UpdateCrosshair_Patch
        {
            public static void Postfix(Hud __instance, Player player)
            {
                if (__instance == null || __instance.m_hoverName == null || player == null) return;

                GameObject hoverObject = player.GetHoverObject();
                if (hoverObject != null)
                {
                    Player targetPlayer = hoverObject.GetComponentInParent<Player>();
                    if (targetPlayer != null && targetPlayer != player)
                    {
                        Color col = NetworkSync.GetPlayerColor(targetPlayer.GetPlayerName());
                        __instance.m_hoverName.color = col;
                        return;
                    }
                }

                // Сброс в белый цвет для дверей, сундуков, костров и предметов
                __instance.m_hoverName.color = Color.white;
            }
        }
    }
}
