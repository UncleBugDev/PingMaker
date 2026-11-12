using System;
using PingMaker.Configuration;
using PingMaker.Network;
using PingMaker.UI;
using UnityEngine;

namespace PingMaker.Core
{
    /// <summary>
    /// Контроллер ввода и пуска луча прицеливания (Raycast) для установки пингов в мир.
    /// Быстрый клик = мгновенный пинг личным цветом игрока.
    /// Зажатие клавиши = вызов кругового радиального меню выбора личного цвета.
    /// </summary>
    public class PingController : MonoBehaviour
    {
        public static PingController? Instance { get; private set; }

        private float m_pressStartTime = -1f;
        private bool m_isHolding;
        private float m_lastPingTime;
        private int m_layerMask;

        public bool IsSuppressingAltAttack => (m_pressStartTime > 0f || m_isHolding) &&
                                              (ModConfig.PingKey.Value == KeyCode.Mouse2 || ModConfig.AlternativePingKey.Value == KeyCode.Mouse2);

        public static void EnsureExists()
        {
            if (Instance != null) return;

            GameObject ctrlObj = new GameObject("PingMaker_Controller");
            DontDestroyOnLoad(ctrlObj);
            Instance = ctrlObj.AddComponent<PingController>();
        }

        private void Awake()
        {
            Instance = this;

            m_layerMask = LayerMask.GetMask(
                "Default",
                "static_solid",
                "Default_small",
                "piece",
                "piece_nonsolid",
                "terrain",
                "character",
                "character_net",
                "item",
                "vehicle",
                "Water"
            );
        }

        private void Update()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                CancelMenu();
                return;
            }

            // Блокировка ввода, если открыт чат, консоль, инвентарь или большая карта
            if (IsInputBlocked())
            {
                CancelMenu();
                return;
            }

            bool keyDown = IsPingKeyDown();
            bool keyStay = IsPingKeyStay();
            bool keyUp = IsPingKeyUp();

            // 1. Начало нажатия
            if (keyDown)
            {
                m_pressStartTime = Time.time;
                m_isHolding = false;
            }

            // 2. Удержание клавиши: открываем радиальное меню палитры личного цвета
            if (keyStay && m_pressStartTime > 0f)
            {
                float heldDuration = Time.time - m_pressStartTime;
                if (!m_isHolding && heldDuration >= ModConfig.HoldTimeForMenu.Value)
                {
                    m_isHolding = true;
                    Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
                    RadialMenu.Instance?.Open(screenCenter);
                }
            }

            // 3. Отпускание клавиши
            if (keyUp && m_pressStartTime > 0f)
            {
                m_pressStartTime = -1f;

                if (m_isHolding)
                {
                    // Подтверждаем и сохраняем выбранный личный цвет
                    RadialMenu.Instance?.ConfirmSelection();
                    m_isHolding = false;
                }
                else
                {
                    // Быстрый клик: ставим пинг активным личным цветом игрока
                    ExecuteRaycastAndSend();
                }
            }
        }

        private void CancelMenu()
        {
            m_pressStartTime = -1f;
            if (m_isHolding)
            {
                m_isHolding = false;
                RadialMenu.Instance?.Hide();
            }
        }

        private bool IsInputBlocked()
        {
            if (Chat.instance != null && Chat.instance.HasFocus()) return true;
            if (Console.IsVisible()) return true;
            if (Minimap.IsOpen()) return true;
            if (InventoryGui.IsVisible()) return true;
            if (Menu.IsVisible()) return true;
            if (StoreGui.IsVisible()) return true;
            return false;
        }

        private bool IsPingKeyDown()
        {
            if (Input.GetKeyDown(ModConfig.PingKey.Value)) return true;
            if (ModConfig.AlternativePingKey.Value != KeyCode.None && Input.GetKeyDown(ModConfig.AlternativePingKey.Value)) return true;
            return false;
        }

        private bool IsPingKeyStay()
        {
            if (Input.GetKey(ModConfig.PingKey.Value)) return true;
            if (ModConfig.AlternativePingKey.Value != KeyCode.None && Input.GetKey(ModConfig.AlternativePingKey.Value)) return true;
            return false;
        }

        private bool IsPingKeyUp()
        {
            if (Input.GetKeyUp(ModConfig.PingKey.Value)) return true;
            if (ModConfig.AlternativePingKey.Value != KeyCode.None && Input.GetKeyUp(ModConfig.AlternativePingKey.Value)) return true;
            return false;
        }

        private void ExecuteRaycastAndSend()
        {
            // Cooldown 0.25 сек для защиты от случайного спама
            if (Time.time - m_lastPingTime < 0.25f) return;
            m_lastPingTime = Time.time;

            Camera cam = Utils.GetMainCamera();
            if (cam == null) return;

            Ray ray = new Ray(cam.transform.position, cam.transform.forward);
            float maxDist = ModConfig.MaxPingDistance.Value;

            if (Physics.Raycast(ray, out RaycastHit hit, maxDist, m_layerMask))
            {
                // Игнорируем попадание в самого локального игрока
                if (Player.m_localPlayer != null && hit.collider.transform.root == Player.m_localPlayer.transform)
                {
                    return;
                }

                Character targetChar = hit.collider.GetComponentInParent<Character>();
                ZDOID targetZDOID = ZDOID.None;
                string targetName = "";
                int targetLevel = 1;
                PingCategory category;

                if (targetChar != null && targetChar != Player.m_localPlayer)
                {
                    // Цель - монстр/существо!
                    category = PingCategory.Creature;
                    targetZDOID = targetChar.GetZDOID();
                    targetName = targetChar.GetHoverName();
                    targetLevel = targetChar.GetLevel();
                }
                else
                {
                    // Попадание в мир (земля, камень, дерево, постройка)
                    category = PingCategory.World;
                }

                // Личный цвет игрока, выбранный в палитре
                Color personalColor = PlayerColors.GetActiveColor();
                string senderName = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Viking";
                float duration = ModConfig.PingDuration.Value;

                Vector3 targetPoint = hit.point;
                if (targetChar != null)
                {
                    targetPoint = targetChar.GetHeadPoint() + Vector3.up * 0.4f;
                }

                PingPackageData data = new PingPackageData(
                    targetPoint,
                    targetZDOID,
                    category,
                    personalColor,
                    senderName,
                    duration,
                    targetName,
                    targetLevel
                );

                NetworkSync.SendPing(data);
            }
        }
    }
}
