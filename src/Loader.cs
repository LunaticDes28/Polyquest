using BepInEx.Logging;
using HarmonyLib;
using PolytopiaBackendBase.Game;
using UnityEngine;
using UnityEngine.EventSystems;
using PolytopiaBackendBase.Common;
using Il2CppInterop.Runtime;
using DG.Tweening;
using Polytopia.Data;


namespace Polyquest
{
    public static class Loader
    {
        public static bool isActive = false;
        public static ManualLogSource? modLogger;

        public static void Load(ManualLogSource logger)
        {
            modLogger = logger;

            Harmony.CreateAndPatchAll(typeof(Main));
            Harmony.CreateAndPatchAll(typeof(Loader));
            Harmony.CreateAndPatchAll(typeof(GameSetup));

            PolyMod.Loader.AddPatchDataType("mapPreset", typeof(MapPreset));
            PolyMod.Loader.AddPatchDataType("mapSize", typeof(MapSize));
            PolyMod.Loader.AddPatchDataType("gameType", typeof(GameType));
            PolyMod.Loader.AddPatchDataType("gameMode", typeof(GameMode));
        
            modLogger?.LogInfo("[Conquest] Mod initialized");
        }
    }
}