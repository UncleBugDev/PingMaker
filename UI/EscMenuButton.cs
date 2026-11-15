using HarmonyLib;
using PingMaker.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PingMaker.UI
{
    /// <summary>
    /// Добавляет аутентичную кнопку вызова настроек PingMaker в главное меню паузы (ESC меню) Valheim.
    /// Кнопка клонирует оригинальный стиль и звуки кнопки "Настройки" (Settings) и аккуратно
    /// встраивается в общий вертикальный список кнопок меню.
    /// </summary>
    public static class EscMenuButton
    {
        private static GameObject? s_buttonObj;

        public static void EnsureButton(Menu menu)
        {
            if (menu == null || menu.m_settingsButton == null) return;
            if (s_buttonObj != null) return;

            Transform parent = menu.m_settingsButton.transform.parent;
            if (parent == null) return;

            try
            {
                // Клонируем ванильную кнопку настроек Valheim
                s_buttonObj = Object.Instantiate(menu.m_settingsButton.gameObject, parent);
                s_buttonObj.name = "PingMaker_MenuButton";

                // Размещаем кнопку сразу после "Настройки" (Settings)
                int settingsIndex = menu.m_settingsButton.transform.GetSiblingIndex();
                s_buttonObj.transform.SetSiblingIndex(settingsIndex + 1);

                // Настраиваем компонент Button
                Button btn = s_buttonObj.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick = new Button.ButtonClickedEvent();
                    btn.onClick.AddListener(() =>
                    {
                        PingSettingsGUI.Instance?.Toggle();
                    });
                }

                // Удаляем любые компоненты локализации Valheim, чтобы они не перетерли наш текст
                MonoBehaviour[] scripts = s_buttonObj.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < scripts.Length; i++)
                {
                    MonoBehaviour script = scripts[i];
                    if (script != null && script.GetType().Name.ToLower().Contains("localize"))
                    {
                        Object.Destroy(script);
                    }
                }

                // Устанавливаем стилизованный текст с руной
                var tmp = s_buttonObj.GetComponentInChildren<TMP_Text>();
                if (tmp != null)
                {
                    tmp.text = "ᚱ PingMaker";
                }
                else
                {
                    var txt = s_buttonObj.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = "ᚱ PingMaker";
                    }
                }

                // Принудительно перестраиваем вертикальный макет кнопок меню
                if (menu.menuEntriesParent != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(menu.menuEntriesParent);
                }

                PingMakerPlugin.Log.LogInfo("[PingMaker] Кнопка настроек успешно добавлена в ESC меню.");
            }
            catch (System.Exception ex)
            {
                PingMakerPlugin.Log.LogWarning($"[PingMaker] Не удалось добавить кнопку в ESC меню: {ex.Message}");
            }
        }

        [HarmonyPatch(typeof(Menu), "Start")]
        private static class Menu_Start_Patch
        {
            private static void Postfix(Menu __instance)
            {
                EnsureButton(__instance);
            }
        }

        [HarmonyPatch(typeof(Menu), "Update")]
        private static class Menu_Update_Patch
        {
            private static void Postfix(Menu __instance)
            {
                EnsureButton(__instance);
            }
        }
    }
}
