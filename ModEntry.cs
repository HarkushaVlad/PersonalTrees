using HarmonyLib;
using StardewModdingAPI;

namespace PersonalTrees
{
    internal sealed class ModEntry : Mod
    {
        public static IModHelper StaticHelper = null!;
        public static IMonitor StaticMonitor = null!;

        public override void Entry(IModHelper helper)
        {
            StaticHelper = helper;
            StaticMonitor = Monitor;

            var harmony = new Harmony(ModManifest.UniqueID);
        }
    }
}