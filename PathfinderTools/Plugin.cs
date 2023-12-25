using BepInEx;
using HarmonyLib;
using BepInEx.Logging;

namespace PathfinderTools
{
    [BepInPlugin(modGUID, modName, modVersion)]
    //[BepInDependency("evaisa.lethallib", BepInDependency.DependencyFlags.HardDependency)]
    public class Plugin : BaseUnityPlugin
    {
        private const string modGUID = "alexthedragon.pathfindertools";
        private const string modName = "PathfinderTools";
        private const string modVersion = "1.0.0";

        private readonly Harmony harmony = new Harmony(modGUID);
        public static ManualLogSource logger;
        private void Awake()
        {
            Plugin.logger = Logger;
            harmony.PatchAll();
            Logger.LogInfo("PathfinderTools loaded correctly!");
        }
    }
}
