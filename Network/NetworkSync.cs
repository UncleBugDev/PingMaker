using System;
using System.Collections.Generic;
using PingMaker.Configuration;
using PingMaker.UI;
using UnityEngine;

namespace PingMaker.Network
{
    public static class NetworkSync
    {
        public const string RPC_PING_NAME = "PingMaker_WorldPing";
        public const string RPC_SYNC_COLOR = "PingMaker_SyncColor";
        public const string RPC_REQUEST_COLORS = "PingMaker_RequestColors";

        private static bool s_isRegistered;

        public static readonly Dictionary<string, Color> PlayerColorsMap = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

        public static void RegisterRPC()
        {
            if (ZRoutedRpc.instance == null)
            {
                PingMakerPlugin.Log.LogWarning("[PingMaker] Невозможно зарегистрировать RPC: ZRoutedRpc.instance == null");
                return;
            }

            if (!s_isRegistered)
            {
                ZRoutedRpc.instance.Register<ZPackage>(RPC_PING_NAME, RPC_PingMaker_WorldPing);
                ZRoutedRpc.instance.Register<string, int>(RPC_SYNC_COLOR, RPC_PingMaker_SyncColor);
                ZRoutedRpc.instance.Register(RPC_REQUEST_COLORS, RPC_PingMaker_RequestColors);
                s_isRegistered = true;
                PingMakerPlugin.Log.LogInfo($"[PingMaker] Сетевые RPC зарегистрированы ({RPC_PING_NAME}, {RPC_SYNC_COLOR}, {RPC_REQUEST_COLORS}).");

                RequestAllPlayerColors();
                BroadcastLocalPlayerColor();
            }
        }

        public static void SendPing(PingPackageData data)
        {
            if (!string.IsNullOrEmpty(data.SenderName))
            {
                PlayerColorsMap[data.SenderName] = data.PingColor;
            }

            if (ZRoutedRpc.instance == null)
            {
                HandleIncomingPing(data);
                return;
            }

            ZPackage pkg = data.ToZPackage();
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_PING_NAME, pkg);
        }

        public static void BroadcastLocalPlayerColor()
        {
            if (Player.m_localPlayer == null) return;

            string localName = Player.m_localPlayer.GetPlayerName();
            int colorIdx = ModConfig.ActiveColorIndex.Value;
            Color chosenColor = PlayerColors.GetColorById(colorIdx).Color;
            PlayerColorsMap[localName] = chosenColor;

            try
            {
                Player.m_localPlayer.GetComponent<ZNetView>()?.GetZDO()?.Set("PingMaker_Color", colorIdx);
            }
            catch { }

            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_SYNC_COLOR, localName, colorIdx);
            }
        }

        public static void RequestAllPlayerColors()
        {
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_REQUEST_COLORS);
            }
        }

        public static Color GetPlayerColor(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                return Color.white;
            }

            if (PlayerColorsMap.TryGetValue(playerName, out Color color))
            {
                return color;
            }

            // Попытка прочесть цвет из экземпляров игроков
            List<Player> all = Player.GetAllPlayers();
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    Player p = all[i];
                    if (p != null && p.GetPlayerName() == playerName)
                    {
                        int c = p.GetComponent<ZNetView>()?.GetZDO()?.GetInt("PingMaker_Color", -1) ?? -1;
                        if (c >= 0 && c < PlayerColors.Palette.Length)
                        {
                            Color found = PlayerColors.Palette[c].Color;
                            PlayerColorsMap[playerName] = found;
                            return found;
                        }
                    }
                }
            }

            // Стабильный детерминированный fallback по хэшу имени
            int fallback = Mathf.Abs(playerName.GetHashCode()) % PlayerColors.Palette.Length;
            Color fallbackColor = PlayerColors.Palette[fallback].Color;
            PlayerColorsMap[playerName] = fallbackColor;
            return fallbackColor;
        }

        private static void RPC_PingMaker_WorldPing(long sender, ZPackage pkg)
        {
            if (PingPackageData.TryDeserialize(pkg, out PingPackageData data))
            {
                if (!string.IsNullOrEmpty(data.SenderName))
                {
                    PlayerColorsMap[data.SenderName] = data.PingColor;
                }
                HandleIncomingPing(data);
            }
        }

        private static void RPC_PingMaker_SyncColor(long sender, string playerName, int colorIndex)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            Color color = PlayerColors.GetColorById(colorIndex).Color;
            PlayerColorsMap[playerName] = color;
        }

        private static void RPC_PingMaker_RequestColors(long sender)
        {
            if (Player.m_localPlayer != null && ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(sender, RPC_SYNC_COLOR, Player.m_localPlayer.GetPlayerName(), ModConfig.ActiveColorIndex.Value);
            }
        }

        private static void HandleIncomingPing(PingPackageData data)
        {
            if (PingManagerUI.Instance != null)
            {
                PingManagerUI.Instance.SpawnPing(data);
            }
        }

        public static void Reset()
        {
            s_isRegistered = false;
            PlayerColorsMap.Clear();
        }
    }
}
