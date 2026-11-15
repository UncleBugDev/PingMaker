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

            // 2. Удержание клавиши: открываем радиальное меню палитры личного цвета (если включено в конфиге)
            if (keyStay && m_pressStartTime > 0f)
            {
                float heldDuration = Time.time - m_pressStartTime;
                if (!m_isHolding && heldDuration >= ModConfig.HoldTimeForMenu.Value)
                {
                    if (ModConfig.EnableRadialColorMenu.Value)
                    {
                        m_isHolding = true;
                        TargetLockonReticle.Instance?.SetTarget(null);
                        Vector2 screenCenter = GetCrosshairScreenPoint();
                        RadialMenu.Instance?.Open(screenCenter);
                    }
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

        /// <summary>
        /// Возвращает точную экранную позицию перекрестия прицела игрока с учетом масштаба GUI и разрешения.
        /// </summary>
        public static Vector3 GetCrosshairScreenPoint()
        {
            if (Hud.instance != null && Hud.instance.m_crosshair != null && Hud.instance.m_crosshair.gameObject.activeInHierarchy)
            {
                return Hud.instance.m_crosshair.transform.position;
            }
            return new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0f);
        }

        /// <summary>
        /// Строит луч из камеры строго через точку перекрестия прицела на экране.
        /// Гарантирует совпадение линии выстрела/метки с прицелом на любом разрешении и масштабе экрана.
        /// </summary>
        public static Ray GetAimRay(Camera cam)
        {
            Vector3 crosshairPos = GetCrosshairScreenPoint();
            return cam.ScreenPointToRay(crosshairPos);
        }

        private void ExecuteRaycastAndSend()
        {
            // Cooldown 0.25 сек для защиты от случайного спама
            if (Time.time - m_lastPingTime < 0.25f) return;
            m_lastPingTime = Time.time;

            Camera cam = Utils.GetMainCamera();
            if (cam == null) return;

            float maxDist = ModConfig.MaxPingDistance.Value;
            Ray aimRay = GetAimRay(cam);

            // 1. Приоритетный поиск цели-существа
            Character? targetChar = FindTargetCreature(cam, maxDist);

            Vector3 targetPoint;
            ZDOID targetZDOID = ZDOID.None;
            string targetName = "";
            int targetLevel = 1;
            PingCategory category;

            if (IsValidTargetCreature(targetChar))
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
                // 2. Стандартный луч в мир по линии прицела (земля, камень, постройка, дерево)
                if (Physics.Raycast(aimRay, out RaycastHit hit, maxDist, m_layerMask))
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
                    targetPoint = aimRay.origin + aimRay.direction * Mathf.Min(maxDist, 100f);
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
        /// Проверяет, является ли существо допустимой целью для захвата прицела и пинга.
        /// Полностью исключает любых игроков (как локального, так и сетевых союзников).
        /// </summary>
        public static bool IsValidTargetCreature([System.Diagnostics.CodeAnalysis.NotNullWhen(true)] Character? ch)
        {
            if (ch == null) return false;
            if (ch.gameObject == null) return false;
            if (ch.IsDead()) return false;
            if (ch.IsPlayer() || ch is Player) return false;
            return true;
        }

        /// <summary>
        /// Прецизионный алгоритм захвата существа точно в перекрестии прицела.
        /// Исключает ложные "улеты" и захваты мобов сбоку или за стенами/преградами.
        /// </summary>
        public static Character? FindTargetCreature(Camera cam, float maxDistance)
        {
            Ray aimRay = GetAimRay(cam);
            Vector3 crosshairScreenPos = GetCrosshairScreenPoint();

            int fullMask = LayerMask.GetMask(
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

            // Уровень 1: Прямой луч через перекрестие (наивысший приоритет)
            if (Physics.Raycast(aimRay, out RaycastHit directHit, maxDistance, fullMask))
            {
                if (Player.m_localPlayer == null || directHit.collider.transform.root != Player.m_localPlayer.transform)
                {
                    Character ch = directHit.collider.GetComponentInParent<Character>();
                    if (ch == null && directHit.collider.attachedRigidbody != null)
                    {
                        ch = directHit.collider.attachedRigidbody.GetComponent<Character>();
                    }

                    if (IsValidTargetCreature(ch))
                    {
                        return ch;
                    }

                    // Если прямой луч попал в твердое препятствие (стена, скала, дерево, земля),
                    // ограничиваем дальность, чтобы не захватывать мобов сквозь преграды!
                    maxDistance = Mathf.Min(maxDistance, directHit.distance);
                }
            }

            // Уровень 2: Ванильный захват цели игрока на ближней дистанции (мгновенный отклик)
            if (Player.m_localPlayer != null)
            {
                Character hover = Player.m_localPlayer.GetHoverCreature();
                if (IsValidTargetCreature(hover))
                {
                    return hover;
                }
            }

            // Уровень 3: Прецизионный поиск существа вблизи центра перекрестия на экране (в радиусе 38 пикселей)
            // Надежно подхватывает цель при небольшом опережении или быстром движении зверя
            List<Character> allChars = Character.GetAllCharacters();
            if (allChars == null || allChars.Count == 0) return null;

            int obstacleMask = LayerMask.GetMask("Default", "static_solid", "piece", "terrain");
            Character? bestCandidate = null;
            float bestScreenDistance = 38f; // Строгий радиус захвата вокруг перекрестия на экране

            for (int i = 0; i < allChars.Count; i++)
            {
                Character ch = allChars[i];
                if (!IsValidTargetCreature(ch)) continue;

                Vector3 charCenter = ch.GetCenterPoint();
                Vector3 screenPoint = cam.WorldToScreenPoint(charCenter);

                // Существо должно находиться перед камерой и ближе ближайшего препятствия
                if (screenPoint.z <= 0.2f || screenPoint.z > maxDistance) continue;

                float distToCrosshair = Vector2.Distance(
                    new Vector2(screenPoint.x, screenPoint.y),
                    new Vector2(crosshairScreenPos.x, crosshairScreenPos.y)
                );

                if (distToCrosshair < bestScreenDistance)
                {
                    // Проверка прямой видимости (нет ли скалы/стены между камерой и существом)
                    Vector3 toChar = charCenter - aimRay.origin;
                    float worldDist = toChar.magnitude;

                    if (!Physics.Raycast(aimRay.origin, toChar.normalized, out _, worldDist - 0.2f, obstacleMask))
                    {
                        bestCandidate = ch;
                        bestScreenDistance = distToCrosshair;
                    }
                }
            }

            return bestCandidate;
        }
    }
}
