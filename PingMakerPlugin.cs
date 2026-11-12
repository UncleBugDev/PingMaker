using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using PingMaker.Configuration;
using PingMaker.Core;
using PingMaker.Network;
using PingMaker.UI;
using UnityEngine;

namespace PingMaker
{
    [BepInPlugin(ModGUID, ModName, ModVersion)]
    public class PingMakerPlugin : BaseUnityPlugin
    {
        public const string ModGUID = "com.author.pingmaker";
        public const string ModName = "PingMaker";
        public const string ModVersion = "1.0.0";

        public static PingMakerPlugin Instance { get; private set; } = null!;
        public static ManualLogSource Log => Instance.Logger;

        private Harmony _harmony = null!;

        private void Awake()
        {
            Instance = this;

            // 1. Инициализация параметров конфигурации
            ModConfig.Init(Config);

            // 2. Инициализация хуков Harmony
            _harmony = new Harmony(ModGUID);
            _harmony.PatchAll();

            Log.LogInfo($"[{ModName}] Мод v{ModVersion} успешно инициализирован.");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }

        [HarmonyPatch(typeof(Game), "Start")]
        private static class GameStartPatch
        {
            private static void Postfix()
            {
                try
                {
                    // Регистрация сетевого RPC
                    NetworkSync.RegisterRPC();

                    // Создание корневого холста UI
                    PingManagerUI.EnsureExists();

                    // Создание радиального меню
                    if (PingManagerUI.Instance != null)
                    {
                        RadialMenu.EnsureExists(PingManagerUI.Instance.transform);
                    }

                    // Создание внутриигровой панели настроек (по кнопке ' / э)
                    PingSettingsGUI.EnsureExists();

                    // Создание контроллера ввода и прицеливания
                    PingController.EnsureExists();

                    Log.LogInfo($"[{ModName}] Системы UI, настроек, ввода и сетевой синхронизации запущены.");
                }
                catch (System.Exception ex)
                {
                    Log.LogError($"[{ModName}] Ошибка при инициализации в Game.Start: {ex}");
                }
            }
        }

        [HarmonyPatch(typeof(Game), "OnDestroy")]
        private static class GameDestroyPatch
        {
            private static void Prefix()
            {
                NetworkSync.Reset();
            }
        }

        [HarmonyPatch(typeof(Player), nameof(Player.SetControls))]
        private static class SuppressAltAttackOnPingPatch
        {
            private static void Prefix(ref bool secondaryAttack, ref bool secondaryAttackHold)
            {
                if (PingController.Instance != null && PingController.Instance.IsSuppressingAltAttack)
                {
                    secondaryAttack = false;
                    secondaryAttackHold = false;
                }
            }
        }

        [HarmonyPatch(typeof(PlayerController), "TakeInput")]
        private static class SuppressPlayerInputWhenSettingsOpenPatch
        {
            private static void Postfix(ref bool __result)
            {
                if (PingSettingsGUI.IsOpen)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(Minimap), "UpdatePins")]
        private static class MinimapUpdatePinsPatch
        {
            private static void Postfix()
            {
                PingManagerUI.Instance?.UpdateMapPinsColor();
            }
        }
    }
}
