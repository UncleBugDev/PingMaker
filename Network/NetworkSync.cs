using System;
using PingMaker.Configuration;
using PingMaker.UI;
using UnityEngine;

namespace PingMaker.Network
{
    public static class NetworkSync
    {
        public const string RPC_PING_NAME = "PingMaker_WorldPing";
        private static bool s_isRegistered;

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
                s_isRegistered = true;
                PingMakerPlugin.Log.LogInfo($"[PingMaker] Сетевой RPC '{RPC_PING_NAME}' успешно зарегистрирован.");
            }
        }

        public static void SendPing(PingPackageData data)
        {
            if (ZRoutedRpc.instance == null)
            {
                // Fallback для локального тестирования
                HandleIncomingPing(data);
                return;
            }

            ZPackage pkg = data.ToZPackage();

            // ZRoutedRpc.Everybody = 0L: мгновенное локальное исполнение + трансляция пирам
            ZRoutedRpc.instance.InvokeRoutedRPC(ZRoutedRpc.Everybody, RPC_PING_NAME, pkg);
        }

        private static void RPC_PingMaker_WorldPing(long sender, ZPackage pkg)
        {
            if (PingPackageData.TryDeserialize(pkg, out PingPackageData data))
            {
                HandleIncomingPing(data);
            }
        }

        private static void HandleIncomingPing(PingPackageData data)
        {
            // Отображение 3D-маркера в мире и временной метки на карте
            if (PingManagerUI.Instance != null)
            {
                PingManagerUI.Instance.SpawnPing(data);
            }
        }

        public static void Reset()
        {
            s_isRegistered = false;
        }
    }
}
