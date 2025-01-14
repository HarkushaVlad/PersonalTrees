using HarmonyLib;
using Microsoft.Xna.Framework;
using Netcode;
using PersonalTrees.Constants;
using StardewValley;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace PersonalTrees.Patches
{
    public class TreeToolActionPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), "performToolAction"),
                prefix: new HarmonyMethod(typeof(TreeToolActionPatch), nameof(Prefix))
            );
        }

        public static bool Prefix(ref bool __result, Tool t, int explosion, Vector2 tileLocation)
        {
            if (t is not Axe)
                return true;

            var tree = Game1.currentLocation.terrainFeatures.ContainsKey(tileLocation)
                ? Game1.currentLocation.terrainFeatures[tileLocation] as Tree
                : null;

            if (tree == null)
                return true;

            if (tree.modData.TryGetValue(ModConstants.TreeOwnerKey, out var ownerId))
            {
                var owner = Game1.getAllFarmers().FirstOrDefault(f => f.UniqueMultiplayerID.ToString() == ownerId);

                if (owner != null && Game1.player.UniqueMultiplayerID.ToString() != ownerId)
                {
                    Game1.currentLocation.playSound(
                        tree.growthStage == new NetInt(1) ? "weed_cut" : "axe",
                        tileLocation
                    );
                    Game1.player.jitterStrength = 1f;

                    var message = ModEntry.StaticHelper.Translation.Get(
                        ModConstants.TranslationKeys.AnotherOwner,
                        new { playerName = owner.Name }
                    );
                    Game1.drawObjectDialogue(message);

                    __result = false;
                    return false;
                }
            }

            return true;
        }
    }
}