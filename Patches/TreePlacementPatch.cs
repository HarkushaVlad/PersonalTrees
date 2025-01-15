using HarmonyLib;
using Microsoft.Xna.Framework;
using PersonalTrees.Constants;
using StardewValley;
using StardewValley.TerrainFeatures;
using Object = StardewValley.Object;

namespace PersonalTrees.Patches
{
    /// <summary>
    /// Patch to handle custom behavior when placing a tree sapling.
    /// </summary>
    public static class TreePlacementPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Object), nameof(Object.placementAction)),
                postfix: new HarmonyMethod(typeof(TreePlacementPatch), nameof(Postfix))
            );
        }

        /// <summary>
        /// Postfix to add custom logic after placing an object in the world.
        /// </summary>
        /// <param name="__result">Indicates if the placement was successful.</param>
        /// <param name="location">The game location where the object was placed.</param>
        /// <param name="x">The X-coordinate of the placement.</param>
        /// <param name="y">The Y-coordinate of the placement.</param>
        /// <param name="who">The farmer performing the placement.</param>
        /// <param name="__instance">The object being placed.</param>
        public static void Postfix(bool __result, GameLocation location, int x, int y, Farmer who, Object __instance)
        {
            if (!__result || __instance == null)
                return;

            if (__instance.IsWildTreeSapling())
            {
                var position = new Vector2(x / 64, y / 64);

                // Check if there is a tree at the placement location
                if (location.terrainFeatures.TryGetValue(position, out var feature) && feature is Tree tree)
                {
                    // Assign the tree's owner as the player who placed it
                    tree.modData[ModConstants.TreeOwnerKey] = who.UniqueMultiplayerID.ToString();
                }
            }
        }
    }
}