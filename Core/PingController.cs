using System;
using System.Collections.Generic;
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
                TargetLockonReticle.Instance?.SetTarget(null);
                return;
            }

            // Блокировка ввода, если открыт чат, консоль, инвентарь или большая карта
            if (IsInputBlocked())
            {
                CancelMenu();
                TargetLockonReticle.Instance?.SetTarget(null);
                return;
            }

            Camera cam = Utils.GetMainCamera();

            // Обновление тактического захвата цели (Lock-on Reticle) при наведении на существо
            if (cam != null && !m_isHolding && ModConfig.TargetReticleMode.Value != 1)
            {
                Character? hoveredCreature = FindTargetCreature(cam, ModConfig.MaxPingDistance.Value);
                TargetLockonReticle.Instance?.SetTarget(hoveredCreature);
            }
            else
            {
                TargetLockonReticle.Instance?.SetTarget(null);
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
                    TargetLockonReticle.Instance?.SetTarget(null);
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
            TargetLockonReticle.Instance?.SetTarget(null);
        }

        private bool IsInputBlocked()
        {
            if (PingSettingsGUI.IsOpen) return true;
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

            float maxDist = ModConfig.MaxPingDistance.Value;

            // 1. Приоритетный поиск цели-существа через 3-уровневый алгоритм
            Character? targetChar = FindTargetCreature(cam, maxDist);

            Vector3 targetPoint;
            ZDOID targetZDOID = ZDOID.None;
            string targetName = "";
            int targetLevel = 1;
            PingCategory category;

            if (targetChar != null && targetChar != Player.m_localPlayer)
            {
                // Захвачено существо!
                category = PingCategory.Creature;
                targetZDOID = targetChar.GetZDOID();
                targetName = targetChar.GetHoverName();
                targetLevel = targetChar.GetLevel();
                targetPoint = GetCreatureTargetPoint(targetChar);
            }
            else
            {
                // 2. Стандартный луч в мир (земля, камень, постройка, дерево)
                Ray ray = new Ray(cam.transform.position, cam.transform.forward);
                if (Physics.Raycast(ray, out RaycastHit hit, maxDist, m_layerMask))
                {
                    if (Player.m_localPlayer != null && hit.collider.transform.root == Player.m_localPlayer.transform)
                    {
                        return;
                    }

                    category = PingCategory.World;
                    targetPoint = hit.point;
                }
                else
                {
                    // Луч ушел в открытый горизонт/небо
                    category = PingCategory.World;
                    targetPoint = cam.transform.position + cam.transform.forward * Mathf.Min(maxDist, 100f);
                }
            }

            // Личный цвет игрока, выбранный в палитре
            Color personalColor = PlayerColors.GetActiveColor();
            string senderName = Player.m_localPlayer != null ? Player.m_localPlayer.GetPlayerName() : "Viking";
            float duration = ModConfig.PingDuration.Value;

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

        /// <summary>
        /// Безопасное получение точки над головой/центром существа для позиционирования пинга.
        /// Защищено от NullReferenceException, если в префабе существа отсутствует ссылка m_head.
        /// </summary>
        public static Vector3 GetCreatureTargetPoint(Character? character)
        {
            if (character == null) return Vector3.zero;

            try
            {
                return character.GetTopPoint() + Vector3.up * 0.2f;
            }
            catch {}

            try
            {
                return character.GetCenterPoint() + Vector3.up * 0.4f;
            }
            catch {}

            return character.transform.position + Vector3.up * 1.5f;
        }

        /// <summary>
        /// 3-уровневый универсальный алгоритм захвата существ.
        /// Не блокируется травой, кустами, листвой или мелкими триггерами.
        /// </summary>
        public static Character? FindTargetCreature(Camera cam, float maxDistance)
        {
            Vector3 camPos = cam.transform.position;
            Vector3 camFwd = cam.transform.forward;

            // Уровень 1: Ванильный захват цели игрока на ближней дистанции (мгновенный отклик)
            if (Player.m_localPlayer != null)
            {
                Character hover = Player.m_localPlayer.GetHoverCreature();
                if (hover != null && hover != Player.m_localPlayer && !hover.IsDead())
                {
                    return hover;
                }
            }

            // Уровень 2: Объемный луч SphereCastAll (радиус 0.9м) ТОЛЬКО по слоям существ
            // Игнорирует 100% травы, кустов, листвы и мелких триггеров
            int charMask = LayerMask.GetMask("character", "character_net");
            RaycastHit[] hits = Physics.SphereCastAll(camPos, 0.9f, camFwd, maxDistance, charMask);
            if (hits != null && hits.Length > 0)
            {
                Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
                int obstacleMask = LayerMask.GetMask("Default", "static_solid", "piece", "terrain");

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider == null) continue;
                    if (Player.m_localPlayer != null && hit.collider.transform.root == Player.m_localPlayer.transform) continue;

                    Character ch = hit.collider.GetComponentInParent<Character>();
                    if (ch == null && hit.collider.attachedRigidbody != null)
                    {
                        ch = hit.collider.attachedRigidbody.GetComponent<Character>();
                    }

                    if (ch != null && ch != Player.m_localPlayer && !ch.IsDead())
                    {
                        // Проверка прямой видимости: не скрыто ли существо за горой или каменной стеной
                        Vector3 toChar = ch.GetCenterPoint() - camPos;
                        if (!Physics.Raycast(camPos, toChar.normalized, out _, toChar.magnitude - 0.2f, obstacleMask))
                        {
                            return ch;
                        }
                    }
                }
            }

            // Уровень 3: Конусный поиск среди Character.GetAllCharacters()
            // Гарантированно ловит быстрых кабанов, оленей в высокой траве и птиц
            List<Character> allChars = Character.GetAllCharacters();
            Character? bestCandidate = null;
            float bestAngle = 4.0f; // Конус 4 градуса вокруг перекрестия

            if (allChars != null)
            {
                int obstacleMask = LayerMask.GetMask("Default", "static_solid", "piece", "terrain");
                for (int i = 0; i < allChars.Count; i++)
                {
                    Character ch = allChars[i];
                    if (ch == null || ch == Player.m_localPlayer || ch.IsDead() || ch.gameObject == null) continue;

                    Vector3 toChar = ch.GetCenterPoint() - camPos;
                    float dist = toChar.magnitude;
                    if (dist > maxDistance || dist < 0.5f) continue;

                    float angle = Vector3.Angle(camFwd, toChar);
                    float maxAllowedAngle = Mathf.Clamp(16.0f / Mathf.Max(2.0f, dist * 0.2f), 1.2f, 4.0f);

                    if (angle < maxAllowedAngle && angle < bestAngle)
                    {
                        if (!Physics.Raycast(camPos, toChar.normalized, out _, dist - 0.2f, obstacleMask))
                        {
                            bestCandidate = ch;
                            bestAngle = angle;
                        }
                    }
                }
            }

            return bestCandidate;
        }
    }
}
