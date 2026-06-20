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
        private static int _customModeIndex = -1;

        // Pinned strictly to prevent memory reclamation while the UI is rendering
        private static Il2CppSystem.Action<int>? _pinnedCallback;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.CreateHorizontalList))]
        private static bool GameSetupScreen_CreateHorizontalList(
            GameSetupScreen __instance, 
            string headerKey, 
            ref Il2CppStringArray items, 
            ref Il2CppSystem.Action<int> indexChangedCallback, 
            int selectedIndex, 
            RectTransform parent, 
            int enabledItemCount, 
            Il2CppSystem.Action onClickDisabledItemCallback)
        {
            if (headerKey == "gamesettings.mode") 
            {
                Loader.modLogger?.LogInfo("[Conquest] Setting up custom game mode list array.");

                List<string> list = items.ToList();
                list.Add("Conquest");
                items = list.ToArray();

                _customModeIndex = list.Count - 1; 

                var originalCallback = indexChangedCallback;

                // ROUTER ONLY: Keep this lean. It only exists to push index 4 forward to the handlers.
                _pinnedCallback = new Action<int>((clickedIndex) => 
                {
                    if (clickedIndex == _customModeIndex)
                    {
                        // Explicitly invoke the postfix methods if the native game routine skips index 4
                        __instance.OnGameModeChanged(_customModeIndex);
                        __instance.OnCustomGameModeChanged(_customModeIndex);
                    }
                    else
                    {
                        originalCallback?.Invoke(clickedIndex);
                    }
                });

                indexChangedCallback = _pinnedCallback;
            }
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnGameModeChanged))]
        public static void OnGameModeChanged_Postfix(GameSetupScreen __instance, int index)
        {
            if (index == _customModeIndex) ProcessCustomModeSelection(__instance);
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnCustomGameModeChanged))]
        public static void OnCustomGameModeChanged_Postfix(GameSetupScreen __instance, int index)
        {
            if (index == _customModeIndex) ProcessCustomModeSelection(__instance);
        }

        private static void ProcessCustomModeSelection(GameSetupScreen instance)
        {
            if (_isProcessingCustomMode) return;

            var settings = GameManager.PreliminaryGameSettings;
            if (settings != null)
            {
                try
                {
                    _isProcessingCustomMode = true;

                    // Centralized settings modification layer
                    settings.BaseGameMode = EnumCache<GameMode>.GetType("conquest");
                    settings.RulesGameMode = EnumCache<GameMode>.GetType("conquest");

                    instance.RefreshInfo();
                    Loader.modLogger!.LogInfo("[Conquest] Settings and UI refreshed successfully.");
                }
                catch (Exception ex)
                {
                    Loader.modLogger!.LogError($"[Conquest] Failure inside centralized UI refresh loop: {ex}");
                }
                finally
                {
                    _isProcessingCustomMode = false;
                }
            }
        }
    }
}
