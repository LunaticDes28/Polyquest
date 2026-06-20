using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays; 

namespace Polyquest
{
    public static class GameSetup
    {
        private static bool _isProcessingCustomMode = false;

        // ====================== PURE UI VISUAL INJECTION ======================
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))]
        private static bool GameSetupScreen_CreateHorizontalList(
            GameSetupScreen __instance, 
            string headerKey, 
            ref Il2CppStringArray items, 
            Il2CppSystem.Action<int> indexChangedCallback, 
            int selectedIndex, 
            RectTransform parent, 
            int enabledItemCount, 
            Il2CppSystem.Action onClickDisabledItemCallback)
        {
            Loader.modLogger?.LogInfo($"[Conquest-UI] CreateHorizontalList caught! HeaderKey: '{headerKey}'");

            if (headerKey == "gamesettings.mode") 
            {
                Loader.modLogger?.LogInfo("[Conquest-UI] Target row 'gamesettings.mode' identified. Reading native array...");
                
                List<string> list = items.ToList();
                Loader.modLogger?.LogInfo($"[Conquest-UI] Vanilla mode items count: {list.Count}. Modes: {string.Join(", ", list)}");

                list.Add("Conquest");
                items = list.ToArray();
                
                Loader.modLogger?.LogInfo($"[Conquest-UI] 'Conquest' successfully appended. New total count: {list.Count}");
            }
            return true; // Pass control back to vanilla layout framework safely
        }

        // ====================== SAFE EVENT POSTFIX TRACKERS ======================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnGameModeChanged))]
        public static void OnGameModeChanged_Postfix(GameSetupScreen __instance, int index)
        {
            Loader.modLogger?.LogInfo($"[Conquest-UI] OnGameModeChanged Postfix fired. Parameter Index: {index}");
            EvaluateGameSetupScreenState(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnCustomGameModeChanged))]
        public static void OnCustomGameModeChanged_Postfix(GameSetupScreen __instance, int index)
        {
            Loader.modLogger?.LogInfo($"[Conquest-UI] OnCustomGameModeChanged Postfix fired. Parameter Index: {index}");
            EvaluateGameSetupScreenState(__instance);
        }

        private static void EvaluateGameSetupScreenState(GameSetupScreen instance)
        {
            Loader.modLogger?.LogInfo("[Conquest-UI] Evaluating UI state via direct component extraction...");

            if (_isProcessingCustomMode)
            {
                Loader.modLogger?.LogWarning("[Conquest-UI] State evaluation blocked: Already processing a UI layout redraw cycle.");
                return;
            }

            if (instance.gameModeList == null)
            {
                Loader.modLogger?.LogError("[Conquest-UI] Aborting evaluation: instance.gameModeList field reference is NULL.");
                return;
            }

            if (instance.gameModeList.items == null)
            {
                Loader.modLogger?.LogError("[Conquest-UI] Aborting evaluation: instance.gameModeList.items unmanaged array pointer is NULL.");
                return;
            }

            // Extract the native highlighted index property (verified uppercase selection)
            int activeVisualIndex = instance.gameModeList.SelectedIndex;
            Loader.modLogger?.LogInfo($"[Conquest-UI] Visual element active highlight position reads index: {activeVisualIndex}");

            // Out-of-bounds structural boundary safety gate
            if (activeVisualIndex < 0 || activeVisualIndex >= instance.gameModeList.items.Count)
            {
                Loader.modLogger?.LogWarning($"[Conquest-UI] Index {activeVisualIndex} falls outside layout boundary bounds (0 to {instance.gameModeList.items.Count - 1}). Skipping.");
                return;
            }

            var activeItem = instance.gameModeList.items[activeVisualIndex];
            if (activeItem == null)
            {
                Loader.modLogger?.LogError($"[Conquest-UI] UI Horizontal item reference at index slot {activeVisualIndex} is NULL.");
                return;
            }

            if (activeItem.text == null)
            {
                Loader.modLogger?.LogError($"[Conquest-UI] Item text component object at index slot {activeVisualIndex} is NULL.");
                return;
            }

            // INTUITIVE TEXT COMPARISON RULE
            string selectedText = activeItem.text.ToString();
            Loader.modLogger?.LogInfo($"[Conquest-UI] Screen item text verified: '{selectedText}'");

            if (selectedText.Equals("Conquest", StringComparison.OrdinalIgnoreCase))
            {
                Loader.modLogger?.LogInfo("[Conquest-UI] MATCH CONFIRMED! Player clicked 'Conquest'. Routing to settings layer...");
                ApplyConquestBackendSettings(instance);
            }
            else
            {
                Loader.modLogger?.LogInfo($"[Conquest-UI] Selection '{selectedText}' does not match target mod name. Mod processing skipped.");
            }
        }

        private static void ApplyConquestBackendSettings(GameSetupScreen instance)
        {
            var settings = GameManager.PreliminaryGameSettings;
            if (settings == null)
            {
                Loader.modLogger?.LogError("[Conquest-UI] Critical Failure: GameManager.PreliminaryGameSettings instance is NULL!");
                return;
            }

            try
            {
                _isProcessingCustomMode = true;
                Loader.modLogger?.LogInfo("[Conquest-UI] State latch locked. Applying Conquest GameMode types to unmanaged cache...");

                // Inject Custom Enum configurations safely
                settings.BaseGameMode = EnumCache<GameMode>.GetType("conquest");
                settings.RulesGameMode = EnumCache<GameMode>.GetType("conquest");
                Loader.modLogger?.LogInfo($"[Conquest-UI] Enums applied. BaseGameMode: {settings.BaseGameMode}, RulesGameMode: {settings.RulesGameMode}");

                // Force layout update execution pipeline
                Loader.modLogger?.LogInfo("[Conquest-UI] Executing instance.RefreshInfo() UI layout redraw protocol...");
                instance.RefreshInfo();
                Loader.modLogger?.LogInfo("[Conquest-UI] RefreshInfo layout execution completed successfully.");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-UI] Crash intercepted inside centralized UI modification loop: {ex}");
            }
            finally
            {
                _isProcessingCustomMode = false;
                Loader.modLogger?.LogInfo("[Conquest-UI] State latch unlocked safely.");
            }
        }
    }
}
