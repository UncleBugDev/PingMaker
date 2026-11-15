using System;
using System.Collections.Generic;
using PingMaker.Configuration;
using PingMaker.UI;
using PingMaker.Visuals;
using UnityEngine;

namespace PingMaker.Network
{
    public static class NetworkSync
    {
        public const string RPC_PING_NAME = "PingMaker_WorldPing";
        public const string RPC_SYNC_COLOR = "PingMaker_SyncColor";
        public const string RPC_REQUEST_COLORS = "PingMaker_RequestColors";

        private static bool s_isRegistered;

        /// <summary>
        /// Словарь подтвержденных цветов игроков (полученных по RPC, из ZDO или из пакетов пингов).
        /// Неподтвержденные дефолтные фоллбэки сюда НЕ кэшируются, чтобы реальный цвет мог примениться в любой момент.
        /// </summary>
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

                if (Player.m_localPlayer != null)
                {
                    BroadcastLocalPlayerColor();
                    RequestAllPlayerColors();
                }
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
            if (string.IsNullOrEmpty(localName)) return;

            int colorIdx = ModConfig.ActiveColorIndex.Value;
            Color chosenColor = PlayerColors.GetColorById(colorIdx).Color;
            PlayerColorsMap[localName] = chosenColor;

            try
            {
                ZDO? zdo = Player.m_localPlayer.GetComponent<ZNetView>()?.GetZDO();
                if (zdo != null)
                {
                    zdo.Set("PingMaker_Color", colorIdx);
                }
            }
            catch { }

            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_SYNC_COLOR, localName, colorIdx);
            }

            if (Minimap.instance != null)
            {
                PlayerMapTracker.UpdateLocalPlayer(Minimap.instance);
            }
        }

        public static void RequestAllPlayerColors()
        {
            if (ZRoutedRpc.instance != null)
            {
                ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_REQUEST_COLORS);
            }
        }

        /// <summary>
        /// Периодическая проверка и синхронизация цветов (вызывается раз в ~6-8 секунд).
        /// Гарантирует целостность ZDO локального игрока и подтягивание цветов всех присутствующих на сервере.
        /// </summary>
        public static void PeriodicSync()
        {
            if (Player.m_localPlayer == null) return;

            try
            {
                // 1. Проверяем, записан ли актуальный цвет в ZDO локального игрока
                ZDO? localZdo = Player.m_localPlayer.GetComponent<ZNetView>()?.GetZDO();
                if (localZdo != null)
                {
                    int currentZdoColor = localZdo.GetInt("PingMaker_Color", -1);
                    if (currentZdoColor != ModConfig.ActiveColorIndex.Value)
                    {
                        localZdo.Set("PingMaker_Color", ModConfig.ActiveColorIndex.Value);
                        BroadcastLocalPlayerColor();
                    }
                }

                // 2. Опрашиваем ZDO подключенных игроков, если их цвета еще не подтверждены в словаре
                if (ZNet.instance != null && ZDOMan.instance != null)
                {
                    List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
                    if (playerList != null)
                    {
                        bool needsRequest = false;
                        for (int i = 0; i < playerList.Count; i++)
                        {
                            string pName = playerList[i].m_name;
                            if (string.IsNullOrEmpty(pName)) continue;

                            if (!PlayerColorsMap.ContainsKey(pName))
                            {
                                ZDO? remoteZdo = ZDOMan.instance.GetZDO(playerList[i].m_characterID);
                                if (remoteZdo != null)
                                {
                                    int c = remoteZdo.GetInt("PingMaker_Color", -1);
                                    if (c >= 0 && c < PlayerColors.Palette.Length)
                                    {
                                        PlayerColorsMap[pName] = PlayerColors.Palette[c].Color;
                                        continue;
                                    }
                                }
                                needsRequest = true;
                            }
                        }

                        if (needsRequest)
                        {
                            RequestAllPlayerColors();
                        }
                    }
                }
            }
            catch
            {
                // Игнорируем сетевые исключения во время загрузки/отключения
            }
        }

        public static Color GetPlayerColor(string playerName)
        {
            if (string.IsNullOrEmpty(playerName))
            {
                return Color.white;
            }

            // Локальный игрок всегда видит свой актуально выбранный цвет
            if (Player.m_localPlayer != null && string.Equals(playerName, Player.m_localPlayer.GetPlayerName(), StringComparison.OrdinalIgnoreCase))
            {
                return PlayerColors.GetActiveColor();
            }

            // 1. Если цвет уже верифицирован (по RPC, ZDO или пингу)
            if (PlayerColorsMap.TryGetValue(playerName, out Color color))
            {
                return color;
            }

            // 2. Попытка прочесть ZDO из глобального списка игроков сервера
            if (ZNet.instance != null && ZDOMan.instance != null)
            {
                List<ZNet.PlayerInfo> playerList = ZNet.instance.GetPlayerList();
                if (playerList != null)
                {
                    for (int i = 0; i < playerList.Count; i++)
                    {
                        if (string.Equals(playerList[i].m_name, playerName, StringComparison.OrdinalIgnoreCase))
                        {
                            ZDO? zdo = ZDOMan.instance.GetZDO(playerList[i].m_characterID);
                            if (zdo != null)
                            {
                                int c = zdo.GetInt("PingMaker_Color", -1);
                                if (c >= 0 && c < PlayerColors.Palette.Length)
                                {
                                    Color found = PlayerColors.Palette[c].Color;
                                    PlayerColorsMap[playerName] = found;
                                    return found;
                                }
                            }
                        }
                    }
                }
            }

            // 3. Попытка прочесть цвет из видимых экземпляров Character / Player
            List<Player> all = Player.GetAllPlayers();
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    Player p = all[i];
                    if (p != null && string.Equals(p.GetPlayerName(), playerName, StringComparison.OrdinalIgnoreCase))
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

            // 4. Полностью детерминированный FNV-1a хэш без зависимости от процесса .NET
            // КРИТИЧЕСКИ ВАЖНО: не кэшируем дефолт в PlayerColorsMap, чтобы при первой синхронизации применился реальный цвет!
            int fallback = GetDeterministicColorIndex(playerName);
            return PlayerColors.Palette[fallback].Color;
        }

        /// <summary>
        /// Детерминированный алгоритм FNV-1a.
        /// Возвращает строго одинаковый индекс цвета (0..7) для одного и того же ника на всех компьютерах,
        /// архитектурах и версиях Windows/Linux (в отличие от string.GetHashCode(), рандомизированного в .NET Core).
        /// </summary>
        public static int GetDeterministicColorIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;

            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < name.Length; i++)
                {
                    hash = (hash ^ (uint)char.ToUpperInvariant(name[i])) * 16777619;
                }
                return (int)(hash % (uint)PlayerColors.Palette.Length);
            }
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

                if (Minimap.instance != null)
                {
                    PlayerMapTracker.UpdateMinimap(Minimap.instance);
                }
            }
        }

        private static void RPC_PingMaker_SyncColor(long sender, string playerName, int colorIndex)
        {
            if (string.IsNullOrEmpty(playerName)) return;
            Color color = PlayerColors.GetColorById(colorIndex).Color;
            PlayerColorsMap[playerName] = color;

            if (Minimap.instance != null)
            {
                PlayerMapTracker.UpdateMinimap(Minimap.instance);
            }
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
