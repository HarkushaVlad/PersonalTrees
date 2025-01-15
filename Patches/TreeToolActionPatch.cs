using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using PersonalTrees.Constants;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace PersonalTrees.Patches
{
    /// <summary>
    /// Patch to handle custom behavior when a tool interacts with a tree.
    /// </summary>
    public static class TreeToolActionPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.performToolAction)),
                prefix: new HarmonyMethod(typeof(TreeToolActionPatch), nameof(Prefix))
            );
        }

        /// <summary>
        /// Prefix to handle custom logic before performing a tool action on a tree.
        /// </summary>
        /// <param name="__result">Indicates if the tool action is successful.</param>
        /// <param name="t">The tool being used.</param>
        /// <param name="explosion">Explosion factor (not used in this case).</param>
        /// <param name="tileLocation">The tile location of the tree being interacted with.</param>
        /// <returns>True if the original method should be executed, false to override it.</returns>
        public static bool Prefix(ref bool __result, Tool t, int explosion, Vector2 tileLocation)
        {
            if (t is not Axe)
                return true;

            // Get the tree at the specified tile location
            var tree = Game1.currentLocation.terrainFeatures.ContainsKey(tileLocation)
                ? Game1.currentLocation.terrainFeatures[tileLocation] as Tree
                : null;

            if (tree == null)
                return true;

            // Check if the tree has an owner
            if (tree.modData.TryGetValue(ModConstants.TreeOwnerKey, out var ownerId))
            {
                var owner = Game1.getAllFarmers().FirstOrDefault(f => f.UniqueMultiplayerID.ToString() == ownerId);

                // If the tree has an owner and the current player is not the owner
                if (owner != null && Game1.player.UniqueMultiplayerID.ToString() != ownerId)
                {
                    // Play the appropriate sound depending on the tree's growth stage
                    Game1.currentLocation.playSound(
                        tree.growthStage == new NetInt(1) ? "weed_cut" : "axe",
                        tileLocation
                    );

                    // Apply a jitter effect to the current player
                    Game1.player.jitterStrength = 1f;

                    // Display a message informing the player they can't cut the tree
                    var message = ModEntry.StaticHelper.Translation.Get(
                        ModConstants.TranslationKeys.WarningAnotherOwner,
                        new { playerName = owner.Name }
                    );
                    Game1.drawObjectDialogue(message);

                    // Set __result to false to prevent the original tool action from executing
                    __result = false;
                    return false;
                }
            }

            // If the tree has no owner or the player is the owner, proceed with the original method
            return true;
        }
    }
}