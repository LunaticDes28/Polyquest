using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System;

namespace Polyquest
{
    public static class UI_2
    {
        // 1. 放棄 Init 與協程，直接改 Hook 畫面顯示的 OnShow 事件
        // 當 OnShow 執行時，Polytopia 內部的 gameModeData 已經 100% 被指派且準備完成
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnShow))]
        public static void OnShow_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-UI] GameSetupScreen.OnShow captured. Injecting options before rendering...");
            InjectConquest(__instance);
        }

        private static void InjectConquest(GameSetupScreen_UI2 instance)
        {
            // 防呆安全檢查
            if (instance == null) return;

            if (instance.gameModeData == null)
            {
                Loader.modLogger?.LogWarning("[Conquest-UI] Injection skipped: instance.gameModeData is still null.");
                return;
            }

            var labels = instance.gameModeData.labels;
            if (labels == null)
            {
                Loader.modLogger?.LogWarning("[Conquest-UI] Injection skipped: gameModeData.labels is null.");
                return;
            }

            // 嚴格比對防重複插入
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i] != null && labels[i].Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                {
                    Loader.modLogger?.LogInfo("[Conquest-UI] Conquest option already exists. Skipping injection.");
                    return;
                }
            }

            // 成功寫入 Polytopia 內部的 IL2CPP 列表
            instance.gameModeData.labels.Add("Conquest");
            Loader.modLogger?.LogInfo($"[Conquest-UI] ✅ Successfully injected Conquest. Total options: {labels.Count}");

            // 2. 呼叫最純粹的原生 Canvas 刷新，完全不進行任何組件轉型
            ForceRefreshUI();  
        }

        private static void ForceRefreshUI()
        {
            try
            {
                // 這是不需要任何實例物件、100% 靜態安全呼叫的 Unity API
                Canvas.ForceUpdateCanvases();
                Loader.modLogger?.LogInfo("[Conquest-UI] Global Canvas.ForceUpdateCanvases() executed successfully.");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogWarning($"[Conquest-UI] Canvas refresh exception: {ex.Message}");
            }
        }

        // 3. 保持原本的狀態監聽與 Flag 設定
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
                    Loader.modLogger?.LogInfo($"[Conquest-UI] Swapped to mode: '{selectedText}' (Index: {index})");

                    if (selectedText.Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    {
                        Loader.modLogger?.LogInfo("[Conquest-UI] Match Found → Enabling custom global backend settings");
                        Loader.SetConquestMode(GameManager.PreliminaryGameSettings, true);
                    }
                    else
                    {
                        if (Loader.IsConquestMode(GameManager.PreliminaryGameSettings))
                        {
                            Loader.modLogger?.LogInfo("[Conquest-UI] Mode shifted away → Clearing custom backend flags");
                            Loader.SetConquestMode(GameManager.PreliminaryGameSettings, false);
                        }
                    }
                }
            }
        }
    }
}
