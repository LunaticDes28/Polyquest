using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.InteropTypes.Arrays; 

namespace Polyquest
{
    public static class GameSetup
    {
        // Tracks our modified string layout so OnGameModeChanged can read it instantly
        private static List<string> cachedGameModes = new List<string>();
        private static bool isConquestSelected = false;

        // ====================== CONQUEST MODE ADDITION ======================

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
            Loader.modLogger?.LogInfo("[Conquest] CreateHorizontalList called");
            if (headerKey == "gamesettings.mode") 
            {
                Loader.modLogger?.LogInfo("[Conquest] List of gamemodes found");

                // Convert the IL2CPP string array to a manageable C# List
                cachedGameModes = items.ToList();
                
                // Directly add the hardcoded mode matching your selection check
                cachedGameModes.Add("Conquest");
                
                // Reassign back to the referenced IL2CPP array for the UI to draw
                items = cachedGameModes.ToArray();
            }
            return true;
        }

        // ====================== INTERCEPT THE CRASH (PREFIX) ======================
        [HarmonyPrefix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnGameModeChanged))]
        private static bool GameSetupScreen_OnGameModeChanged_Prefix(GameSetupScreen __instance, ref int index)
        {
            string selectedName = string.Empty;
            if (index >= 0 && index < cachedGameModes.Count)
            {
                selectedName = cachedGameModes[index];
            }

            if (!string.IsNullOrEmpty(selectedName) && selectedName.Equals("Conquest", System.StringComparison.OrdinalIgnoreCase))
            {
                // Set a flag telling our Postfix that Conquest was chosen
                isConquestSelected = true;

                // Change index to 0 so the native game code runs smoothly without throwing an out-of-bounds error
                index = 0; 
            }
            else
            {
                isConquestSelected = false;
            }

            return true; 
        }

        // ====================== APPLY CONQUEST DATA (POSTFIX) ======================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameSetupScreen), nameof(GameSetupScreen.OnGameModeChanged))]
        private static void GameSetupScreen_OnGameModeChanged_Postfix(GameSetupScreen __instance, int index)
        {
            // This runs AFTER the game has processed its logic. 
            // If the flag is true, we now safely force our custom mode data.
            if (isConquestSelected)
            {
                Loader.modLogger?.LogInfo("[Conquest] Mode successfully forced in Postfix!");

                var settings = GameManager.PreliminaryGameSettings;
                if (settings != null)
                {
                    settings.BaseGameMode = EnumCache<GameMode>.GetType("conquest");
                    settings.RulesGameMode = EnumCache<GameMode>.GetType("conquest");
                }
            }
        }

        // ====================== ORIGINAL UI HELPERS ======================

        private static UIHorizontalList? FindHorizontalList(GameSetupScreen screen, string headerKey)
        {
            if (screen?.rows == null) return null;

            // In IL2CPP, screen.rows is an Il2CppReferenceArray and can be iterated directly
            foreach (GameObject row in screen.rows)
            {
                if (row != null && row.TryGetComponent<UIHorizontalList>(out var list) && list.HeaderKey == headerKey)
                {
                    return list;
                }
            }
            return null;
        }

        private static string GetSelectedModeName(GameSetupScreen screen, int index)
        {
            UIHorizontalList? list = FindHorizontalList(screen, "gamesettings.mode");
            
            // Safe bounds-check against the actual visual UI array items
            if (list?.items != null && index >= 0 && index < list.items.Length)
            {
                UIHorizontalListItem selectedItem = list.items[index];
                if (selectedItem != null && selectedItem.text != null)
                {
                    return selectedItem.text.ToString();
                }
            }
            return string.Empty;
        }
    }
}