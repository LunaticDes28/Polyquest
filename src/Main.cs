using HarmonyLib;
using PolytopiaBackendBase.Game;
using Polytopia.Data;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Polyquest
{
    public static class Main
    {
        // =========================================================================
        // A. Map Generation Hook
        // =========================================================================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(MapGenerator), nameof(MapGenerator.GenerateInternal))]
        private static void GenerateInternal_Postfix(MapGenerator __instance, GameState gameState, MapGeneratorSettings settings)
        {
            if (gameState?.Settings == null) return;

            try
            {
                bool isConquest = UI_2.IsConquestSelected;
                if (!isConquest) return;

                Loader.modLogger?.LogInfo("[Conquest-Map] Conquest Mode detected! Aligning landscape structures...");

                // Emergency spawn + ruin conversion
                ConquestVillageGeneration(__instance, gameState);

                // Lock game mode
                int registeredConquestId = PolyMod.Registry.gameModesAutoidx - 1;
                gameState.Settings.RulesGameMode = (GameMode)registeredConquestId;

                UI_2.IsConquestSelected = false;

                Loader.modLogger?.LogInfo($"[Conquest-Map] Map features processed. RulesGameMode stamped as ID: {registeredConquestId}");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Map] MapGenerator detour error: {ex.Message}");
            }
        }

        // =========================================================================
        // B. Emergency Spawn + Ruin Conversion (in GenerateInternal)
        // =========================================================================
        private static void ConquestVillageGeneration(MapGenerator gen, GameState gameState)
        {
            try
            {
                List<TileData> neutralVillages = new List<TileData>();
                for (int i = 0; i < gameState.Map.Tiles.Length; i++)
                {
                    TileData tile = gameState.Map.Tiles[i];
                    if (tile.HasImprovement(ImprovementData.Type.City) && tile.owner == 0)
                    {
                        neutralVillages.Add(tile);
                    }
                }

                int playerCount = gameState.PlayerCount;
                if (playerCount <= 0) return;

                int remainder = neutralVillages.Count % playerCount;

                // Emergency villages
                if (remainder > 0 && remainder >= (playerCount * 0.6f))
                {
                    int citiesToSpawn = playerCount - remainder;
                    Loader.modLogger!.LogInfo($"[Conquest-Map] Spawning {citiesToSpawn} emergency villages...");

                    for (int s = 0; s < citiesToSpawn; s++)
                    {
                        WorldCoordinates emergencyCoords = gen.GetEmergencyCityPosition(gameState, gameState.Map);
                        if (emergencyCoords != WorldCoordinates.NULL_COORDINATES)
                        {
                            int tileIndex = gameState.Map.GetTileIndex(emergencyCoords);
                            TileData targetTile = gameState.Map.Tiles[tileIndex];
                            gen.SetTileAsCity(targetTile);
                            neutralVillages.Add(targetTile);
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // Calculate which villages should be kept vs turned into ruins
                int maxCitiesPerPlayer = neutralVillages.Count / playerCount;
                HashSet<WorldCoordinates> assignedCoordinates = new HashSet<WorldCoordinates>();

                for (int round = 0; round < maxCitiesPerPlayer; round++)
                {
                    for (int p = 0; p < playerCount; p++)
                    {
                        PlayerState player = gameState.PlayerStates[p];
                        WorldCoordinates capitalCoords = player.startTile;

                        TileData closestVillage = null;
                        int closestDistance = int.MaxValue;

                        foreach (var village in neutralVillages)
                        {
                            if (assignedCoordinates.Contains(village.coordinates)) continue;

                            int distance = MapDataExtensions.ManhattanDistance(capitalCoords, village.coordinates);
                            if (distance < closestDistance)
                            {
                                closestDistance = distance;
                                closestVillage = village;
                            }
                        }

                        if (closestVillage != null)
                        {
                            assignedCoordinates.Add(closestVillage.coordinates);
                        }
                    }
                }

                // Convert leftovers to ruins
                int ruinsCount = 0;
                foreach (var village in neutralVillages)
                {
                    if (!assignedCoordinates.Contains(village.coordinates))
                    {
                        village.improvement = new ImprovementState { type = ImprovementData.Type.Ruin, level = 1 };
                        ruinsCount++;
                    }
                }

                Loader.modLogger!.LogInfo($"[Conquest-Map] Frontier consolidation complete. Converted {ruinsCount} villages to ruins.");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Map] ProcessMapConquestLandmarks failed: {ex.Message}");
            }
        }

        // =========================================================================
        // C. Full City Initialization in StartMatchAction
        // =========================================================================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(StartMatchAction), "ExecuteDefault")]
        private static void StartMatchAction_ExecuteDefault_Postfix(StartMatchAction __instance, GameState gameState)
        {
            if (gameState?.Settings == null) return;

            try
            {
                int registeredConquestId = PolyMod.Registry.gameModesAutoidx - 1;
                if ((int)gameState.Settings.RulesGameMode != registeredConquestId) return;

                Loader.modLogger?.LogInfo("[Conquest-Match] Executing live proximity grid calculations...");

                ConquestVillageDistribution(gameState);
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Match] Critical failure in StartMatchAction: {ex.Message}");
            }
        }

        private static void ConquestVillageDistribution(GameState gameState)
        {
            List<TileData> neutralVillages = new List<TileData>();
            for (int i = 0; i < gameState.Map.Tiles.Length; i++)
            {
                TileData tile = gameState.Map.Tiles[i];
                if (tile.HasImprovement(ImprovementData.Type.City) && tile.owner == 0)
                {
                    neutralVillages.Add(tile);
                }
            }

            int playerCount = gameState.PlayerCount;
            if (playerCount == 0) return;

            int maxCitiesPerPlayer = neutralVillages.Count / playerCount;
            HashSet<WorldCoordinates> assignedCoordinates = new HashSet<WorldCoordinates>();

            Loader.modLogger?.LogInfo($"[Conquest-Match] Dynamic pool: {neutralVillages.Count} villages. Allocating {maxCitiesPerPlayer} per player...");

            for (int round = 0; round < maxCitiesPerPlayer; round++)
            {
                for (int p = 0; p < playerCount; p++)
                {
                    PlayerState player = gameState.PlayerStates[p];
                    WorldCoordinates capitalCoords = player.startTile;

                    TileData closestVillage = null;
                    int closestDistance = int.MaxValue;

                    foreach (var village in neutralVillages)
                    {
                        if (assignedCoordinates.Contains(village.coordinates)) continue;

                        int distance = MapDataExtensions.ManhattanDistance(capitalCoords, village.coordinates);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestVillage = village;
                        }
                    }

                    if (closestVillage != null)
                    {
                        assignedCoordinates.Add(closestVillage.coordinates);
                        ConquestInitializeCity(gameState, closestVillage, player);
                    }
                }
            }

            Loader.modLogger?.LogInfo($"[Conquest-Match] All cities physicalized successfully!");
        }

        // =========================================================================
        // D. Full City Initialization
        // =========================================================================
        private static void ConquestInitializeCity(GameState state, TileData tile, PlayerState player)
        {
            try
            {
                tile.owner = player.Id;
                tile.capitalOf = 0;

                TribeData tribeData;
                if (state.GameLogicData.TryGetData(player.tribe, out tribeData) && tribeData != null)
                {
                    string generatedName = MapDataExtensions.GenerateCityName(state, tile.coordinates, tribeData, player.skinType);
                    if (tile.improvement != null)
                    {
                        tile.improvement.name = generatedName;
                    }
                }

                player.cities++;

                Il2CppSystem.Collections.Generic.List<TileData> cityArea = ActionUtils.GetCityAreaSorted(state, tile);
                if (cityArea != null)
                {
                    for (int j = 0; j < cityArea.Count; j++)
                    {
                        TileData territoryTile = cityArea[j];
                        if (territoryTile != null)
                        {
                            territoryTile.owner = player.Id;
                            territoryTile.rulingCityCoordinates = tile.coordinates;
                        }
                    }
                }

                ActionUtils.RuleArea(state, player, tile, false);
                ActionUtils.ExploreFromTile(state, player, tile, 2, false);

                Loader.modLogger?.LogInfo($"[Conquest-Match] City physicalized for Player {player.Id} at {tile.coordinates}.");
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Match] City physicalization failed: {ex.Message}");
            }
        }

        // =========================================================================
        // E. Tech Cost & City Destruction
        // =========================================================================
        [HarmonyPostfix]
        [HarmonyPatch(typeof(GameLogicData), nameof(GameLogicData.GetTechPrice))]
        private static void Conquest_TechCost_Postfix(GameLogicData __instance, TechData techData, PlayerState playerState, GameState state, ref int __result)
        {
            if (state == null || techData == null) return;
            try
            {
                int registeredConquestId = PolyMod.Registry.gameModesAutoidx - 1;
                if ((int)state.Settings.RulesGameMode != registeredConquestId) return;

                int addition = (int)(4 + state.CurrentTurn);
                addition = Math.Min(addition, 20 + techData.cost * 5);
                __result = (int)Math.Ceiling((double)(techData.cost + addition));
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-Tech] Error: {ex.Message}");
            }
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CaptureCityAction), nameof(CaptureCityAction.ExecuteDefault))]
        private static bool Conquest_CaptureCityAction_Prefix(CaptureCityAction __instance, GameState gameState)
        {
            if (gameState?.Settings == null) return true;
            try
            {
                int registeredConquestId = PolyMod.Registry.gameModesAutoidx - 1;
                if ((int)gameState.Settings.RulesGameMode != registeredConquestId) return true;

                TileData cityTile = gameState.Map.GetTile(__instance.Coordinates);
                PlayerState attacker = null;
                gameState.TryGetPlayer(__instance.PlayerId, out attacker);

                if (cityTile != null && attacker != null)
                    DestroyCityConquest(gameState, cityTile, attacker);

                return false;
            }
            catch
            {
                return true;
            }
        }

        private static void DestroyCityConquest(GameState gameState, TileData cityTile, PlayerState attacker)
        {
            if (cityTile?.improvement?.type != ImprovementData.Type.City) return;

            if (cityTile.owner != 0)
            {
                PlayerState originalOwner;
                if (gameState.TryGetPlayer(cityTile.owner, out originalOwner) && originalOwner != null)
                {
                    if (originalOwner.cities > 0)
                    {
                        originalOwner.cities--;
                        Loader.modLogger?.LogInfo($"[Conquest] Player {originalOwner.Id} lost a city. Total remaining: {originalOwner.cities}");
                    }
                }
            }

            int reward = cityTile.improvement.level * 2 + (int)gameState.CurrentTurn;
            if (attacker != null)
            {
                attacker.Currency += reward;
                Loader.modLogger?.LogInfo($"[Conquest] City destroyed by player {attacker.Id} (+{reward} stars)");
            }

            Il2CppSystem.Collections.Generic.List<TileData> cityArea = ActionUtils.GetCityAreaSorted(gameState, cityTile);
            if (cityArea != null)
            {
                for (int j = 0; j < cityArea.Count; j++)
                {
                    TileData territoryTile = cityArea[j];
                    if (territoryTile != null)
                    {
                        territoryTile.owner = 0;
                        
                        territoryTile.rulingCityCoordinates = WorldCoordinates.NULL_COORDINATES; 
                    }
                }
            }

            bool leaveRuin = UnityEngine.Random.value <= 1f;
            if (leaveRuin)
            {
                cityTile.improvement = new ImprovementState { type = ImprovementData.Type.Ruin, level = 1 };
            }
            else
            {
                cityTile.improvement = null;
            }

            cityTile.owner = 0;
            cityTile.capitalOf = 0;
            
            Loader.modLogger?.LogInfo($"[Conquest] City at {cityTile.coordinates} has been successfully razed.");
        }

        // =========================================================================
        // F. AI interpretation
        // =========================================================================
        [HarmonyPrefix]
        [HarmonyPatch(typeof(AI), nameof(AI.GetGameProgress))]
        private static bool GetGameProgress_Prefix(ref float __result, GameState gameState, PlayerState winningPlayer)
        {
            if (gameState?.Settings == null) return true;

            try
            {
                int registeredConquestId = PolyMod.Registry.gameModesAutoidx - 1;

                if ((int)gameState.Settings.RulesGameMode == registeredConquestId)
                {
                    if (winningPlayer == null)
                    {
                        __result = 0f;
                        return false;
                    }

                    float totalCities = Math.Max(0.1f, (float)MapDataExtensions.CountCities(gameState));
                    float cityProgress = (float)winningPlayer.cities / totalCities;
                    
                    __result = Math.Min(1f, Math.Max(0f, cityProgress));
                    
                    return false; 
                }
            }
            catch (Exception ex)
            {
                Loader.modLogger?.LogError($"[Conquest-AI] Error in GetGameProgress detour: {ex.Message}");
                __result = 0f; 
                return false; 
            }

            return true; 
        }
    }
}