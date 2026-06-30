using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System;

namespace Polyquest
{
    public static class UI_2
    {
        // ==========================================
        // 1. INIT HOOKS (前置與後置)
        // ==========================================
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.Init))]
        public static void Init_Prefix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] >>> [Init Prefix] Triggered.");
            InspectInternalState(__instance, "Init Prefix");
            
            // 嘗試在此進行前置注入實驗
            TryInjectData(__instance, "Init Prefix");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.Init))]
        public static void Init_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] <<< [Init Postfix] Triggered.");
            InspectInternalState(__instance, "Init Postfix");
        }

        // ==========================================
        // 2. ONSHOW HOOKS (前置與後置)
        // ==========================================
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnShow))]
        public static void OnShow_Prefix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] >>> [OnShow Prefix] Triggered.");
            InspectInternalState(__instance, "OnShow Prefix");

            // 如果 Init 沒成功，嘗試在此進行前置注入實驗
            TryInjectData(__instance, "OnShow Prefix");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnShow))]
        public static void OnShow_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] <<< [OnShow Postfix] Triggered.");
            InspectInternalState(__instance, "OnShow Postfix");
        }

        // ==========================================
        // 3. RUNLAYOUT HOOKS (前置與後置)
        // ==========================================
        // 註：因為 RunLayout 在 C# 代理類可能為 protected，若 nameof 報錯，請換成字串 "RunLayout"
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), "RunLayout")]
        public static void RunLayout_Prefix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] >>> [RunLayout Prefix] Triggered.");
            InspectInternalState(__instance, "RunLayout Prefix");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), "RunLayout")]
        public static void RunLayout_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] <<< [RunLayout Postfix] Triggered.");
        }

        // ==========================================
        // 4. ONGAMEMODECHANGED HOOK (狀態監聽)
        // ==========================================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnGameModeChanged))]
        public static void OnGameModeChanged_Postfix(GameSetupScreen_UI2 __instance, int index)
        {
            Loader.modLogger?.LogInfo($"[Conquest-Diag] ⚡ [OnGameModeChanged] Captured! Index Arg: {index}");
            InspectInternalState(__instance, "OnGameModeChanged");
            EvaluateGameSetupScreenState(__instance, index);
        }

        // ==========================================
        // 核心診斷工具：每幀與每事件狀態透視
        // ==========================================
        private static void InspectInternalState(GameSetupScreen_UI2 instance, string stageName)
        {
            if (instance == null)
            {
                Loader.modLogger?.LogWarning($"[Conquest-Diag] [{stageName}] Target instance is NULL.");
                return;
            }

            try
            {
                // 1. 檢查原生 C++ 記憶體指標是否存在
                Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] Instance IL2CPP Pointer: 0x{instance.Pointer.ToInt64():X}");

                // 2. 檢查 view 表現層物件是否存在
                // (利用反射或欄位讀取看它有沒有分配指針)
                if (instance.view == null || instance.view.Pointer == IntPtr.Zero)
                {
                    Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] -> [FIELD] view is CURRENTLY NULL/EMPTY.");
                }
                else
                {
                    Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] -> [FIELD] view IS INSTANTIATED (0x{instance.view.Pointer.ToInt64():X})");
                }

                // 3. 檢查 gameModeData 數據層狀態
                if (instance.gameModeData == null || instance.gameModeData.Pointer == IntPtr.Zero)
                {
                    Loader.modLogger?.LogWarning($"[Conquest-Diag] [{stageName}] -> [FIELD] gameModeData is CURRENTLY NULL/EMPTY.");
                    return;
                }

                Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] -> [FIELD] gameModeData Pointer: 0x{instance.gameModeData.Pointer.ToInt64():X}");

                // 4. 檢查 labels 陣列內容與數量
                var labels = instance.gameModeData.labels;
                if (labels == null || labels.Pointer == IntPtr.Zero)
                {
                    Loader.modLogger?.LogWarning($"[Conquest-Diag] [{stageName}] -> [DATA] gameModeData.labels list is NULL.");
                    return;
                }

                Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] -> [DATA] labels.Count = {labels.Count}");
                
                // 列印出當前菜單裡的所有內容
                for (int i = 0; i < labels.Count; i++)
                {
                    if (labels[i] != null)
                    {
                        Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}]     └─ Slot [{i}]: '{labels[i]}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Diag] [{stageName}] Diagnostics exception encountered: {ex.Message}");
            }
        }

        // ==========================================
        // 核心注入工具：安全嘗試寫入
        // ==========================================
        private static void TryInjectData(GameSetupScreen_UI2 instance, string stageName)
        {
            try
            {
                if (instance == null || instance.gameModeData == null || instance.gameModeData.Pointer == IntPtr.Zero) return;
                var labels = instance.gameModeData.labels;
                if (labels == null || labels.Pointer == IntPtr.Zero) return;

                for (int i = 0; i < labels.Count; i++)
                {
                    if (labels[i] != null && labels[i].Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    {
                        Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] Injection Check: 'Conquest' already listed. Skipping.");
                        return;
                    }
                }

                labels.Add("Conquest");
                Loader.modLogger?.LogInfo($"[Conquest-Diag] [{stageName}] 🚀 DATA INJECTED SUCCESSFULLY. New Count: {labels.Count}");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Diag] [{stageName}] Injection trial crashed: {ex.Message}");
            }
        }

        // ==========================================
        // 後台邏輯 Flag 處理
        // ==========================================
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
                        Loader.modLogger?.LogInfo("[Conquest-Diag] Global Flag Sync -> CONQUEST ENABLED");
                    }
                    else
                    {
                        if (Loader.IsConquestMode(GameManager.PreliminaryGameSettings))
                        {
                            Loader.SetConquestMode(GameManager.PreliminaryGameSettings, false);
                            Loader.modLogger?.LogInfo("[Conquest-Diag] Global Flag Sync -> CONQUEST DISABLED");
                        }
                    }
                }
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnHide))]
        public static void OnHide_Postfix(GameSetupScreen_UI2 __instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-Diag] [OnHide Postfix] Screen closed.");
        }
    }
}
