using HarmonyLib;
using Microsoft.Xna.Framework;
using PersonalTrees.Constants;
using StardewModdingAPI;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace PersonalTrees.Patches
{
    public class TreePlacementPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), "placementAction"),
                postfix: new HarmonyMethod(typeof(TreePlacementPatch), nameof(Postfix))
            );
        }

        public static void Postfix(bool __result, GameLocation location, int x, int y, Farmer who,
            Object __instance)
        {
            if (!__result || __instance == null)
                return;

            if (__instance.IsWildTreeSapling())
            {
                var position = new Vector2(x / 64, y / 64);

                if (location.terrainFeatures.TryGetValue(position, out var feature) && feature is Tree tree)
                {
                    tree.modData[ModConstants.TreeOwnerKey] = who.UniqueMultiplayerID.ToString();
                }
            }
        }
    }
}