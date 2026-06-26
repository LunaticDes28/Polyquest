using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine.EventSystems;

namespace Polyquest
{
    public static class UI_2
    {
        // internal static bool conquestSelected = false;

        private static void InjectConquest(GameSetupScreen_UI2 instance = null)
        {
            if (instance == null)
            {
                if (UIManager.Instance == null) return;
                IScreen screen = UIManager.Instance.GetScreen(UIConstants.Screens.GameSetup);
                if (screen == null) return;
                instance = screen.Cast<GameSetupScreen_UI2>();
                if (instance == null) return;
            }

            if (instance.gameModeData?.labels == null) return;

            var labels = instance.gameModeData.labels;

            // Check if Conquest already exists
            for (int i = 0; i < labels.Count; i++)
            {
                if (labels[i].Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    return;
            }

            instance.gameModeData.labels.Add("Conquest");
            Loader.modLogger?.LogInfo($"[Conquest-UI] ✅ Injected Conquest. Total labels: {labels.Count}");
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen_UI2), nameof(GameSetupScreen_UI2.OnGameModeChanged))]
        public static void OnGameModeChanged_Postfix(GameSetupScreen_UI2 __instance, int index)
        {
            Loader.modLogger?.LogInfo($"[Conquest-UI] OnGameModeChanged Postfix event captured. Raw index: {index}");
            InjectConquest(__instance);
            EvaluateGameSetupScreenState(__instance, index);
        }

        private static void EvaluateGameSetupScreenState(GameSetupScreen_UI2 instance, int index)
        {
            Loader.modLogger?.LogInfo("[Conquest-UI] Inspecting visual UI elements selection components...");

            if (instance.gameModeData == null)
            {
                Loader.modLogger?.LogError("[Conquest-UI] Aborting check: instance.gameModeList is NULL reference.");
                return;
            }

            // int activeVisualIndex = instance.gameModeData.selectedObject;
            Loader.modLogger?.LogInfo($"[Conquest-UI] Current active menu highlighted item index reads: {index}");

            // Loader.modLogger?.LogInfo($"[Conquest-UI] Test Values: {instance.gameModeData.selectedObject}");
            Loader.modLogger?.LogInfo($"[Conquest-UI] Total Label Counts: {instance.gameModeData.labels.Count}");
            
            if (index >= 0 && index < instance.gameModeData.labels.Count)
            {
                var activeItem = instance.gameModeData.labels[index];
                if (activeItem != null && activeItem != null)
                {
                    string selectedText = activeItem.ToString();
                    Loader.modLogger?.LogInfo($"[Conquest-UI] Extracted visual text string from highlighted item slot: '{selectedText}'");

                    // If the text label matches, call the separated custom settings applicator handler
                    if (selectedText.Equals("Conquest", StringComparison.OrdinalIgnoreCase))
                    {
                        Loader.modLogger?.LogInfo("[Conquest-UI] Conquest selected → Setting flag");
                        Loader.SetConquestMode(GameManager.PreliminaryGameSettings, true);
                        Loader.modLogger?.LogInfo("[Conquest-UI] SUCCESS: Conquest mode dictionary flag successfully set to TRUE.");
                    }
                    else
                    {
                        Loader.modLogger?.LogInfo("[Conquest-UI] NOT Conquest selected");
                        if (Loader.IsConquestMode(GameManager.PreliminaryGameSettings))
                        {
                            Loader.modLogger?.LogInfo($"[Conquest-UI] Switched away from Conquest → Resetting flag");
                            Loader.SetConquestMode(GameManager.PreliminaryGameSettings, false);
                            Loader.modLogger?.LogInfo("[Conquest-UI] SUCCESS: Conquest mode dictionary flag successfully set to FALSE.");                        
                        }
                    }
                }
            }
            else
            {
                Loader.modLogger?.LogWarning($"[Conquest-UI] Active index {index} is out of bounds (0 to {instance.gameModeData.labels.Count - 1}). skipping.");
            }
        }
    }
}
