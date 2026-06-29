using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System;

namespace Polyquest
{
    public static class UI_2
    {
        // 儲存當前畫面的靜態引用，完全避免在 OnShow 當下進行高風險的記憶體存取
        private static GameSetupScreen_UI2 _cachedInstance;

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnShow))]
        public static void OnShow_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-UI] GameSetupScreen.OnShow captured. Caching screen instance...");
            _cachedInstance = __instance;
        }

        // 💡 關鍵救星：改 Hook Update (每幀執行的邏輯更新)
        // 這樣可以不斷偵測，直到 Polytopia 自己把 gameModeData 的資料完全初始化完成的那一幀，才精準切入！
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), "Update")] // 如果混淆名稱不同，請確保名稱正確
        public static void Update_Postfix(GameSetupScreen_UI2 __instance)
        {
            if (_cachedInstance == null || _cachedInstance.Pointer != __instance.Pointer) return;

            // 執行最安全的防禦性注入
            TrySecureInjection(__instance);
        }

        private static void TrySecureInjection(GameSetupScreen_UI2 instance)
        {
            try
            {
                // 1. 如果底層記憶體指標還是空的，直接安全退出，等待下一幀
                if (instance.gameModeData == null || instance.gameModeData.Pointer == IntPtr.Zero)
                {
                    return; 
                }

                var labels = instance.gameModeData.labels;
                if (labels == null || labels.Pointer == IntPtr.Zero)
                {
                    return;
                }

                // 2. 檢查是否已經注入過
                for (int i = 0; i < labels.Count; i++)
                {
                    if (labels[i] != null && labels[i].Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    {
                        // 已經注入成功，清除快取，停止後續每幀的 Update 偵測
                        _cachedInstance = null;
                        Loader.modLogger?.LogInfo("[Conquest-UI] Injection verified active. Unregistering Update hook loop.");
                        return;
                    }
                }

                // 3. 執行資料寫入
                labels.Add("Conquest");
                Loader.modLogger?.LogInfo($"[Conquest-UI] ✅ Successfully injected Conquest via Safe Update Loop! Total: {labels.Count}");

                // 4. 用全域 Canvas 強制重新整理
                Canvas.ForceUpdateCanvases();
                
                // 成功後清除引用
                _cachedInstance = null;
            }
            catch (Exception ex)
            {
                // 如果仍有非預期的例外，至少這次能抓到 Log 
                Loader.modLogger?.LogError($"[Conquest-UI] Fatal error during update iteration: {ex.Message}\n{ex.StackTrace}");
                _cachedInstance = null; // 發生異常也強制中止防止洗版
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnHide))]
        public static void OnHide_Postfix()
        {
            // 畫面關閉時，確保快取被清空
            _cachedInstance = null;
        }

        // 保持原本的狀態監聽與 Flag 設定
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnGameModeChanged))]
        public static void OnGameModeChanged_Postfix(GameSetupScreen_UI2 __instance, int index)
        {
            EvaluateGameSetupScreenState(__instance, index);
        }

        private static void EvaluateGameSetupScreenState(GameSetupScreen_UI2 instance, int index)
        {
            if (instance.gameModeData == null || instance.gameModeData.labels == null) return;

            if (index >= 0 && index < instance.gameModeData.labels.Count)
            {
                var activeItem = instance.gameModeData.labels[index];
                if (activeItem != null)
                {
                    string selectedText = activeItem.ToString();
                    if (selectedText.Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    {
                        Loader.SetConquestMode(GameManager.PreliminaryGameSettings, true);
                    }
                    else
                    {
                        if (Loader.IsConquestMode(GameManager.PreliminaryGameSettings))
                        {
                            Loader.SetConquestMode(GameManager.PreliminaryGameSettings, false);
                        }
                    }
                }
            }
        }
    }
}
