using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PersonalTrees.Constants;
using StardewValley;
using StardewValley.Menus;
using StardewValley.TerrainFeatures;

namespace PersonalTrees.Patches
{
    /// <summary>
    /// Patch to add custom drawing for trees, displaying the tree owner's name.
    /// </summary>
    public static class TreeDrawPatch
    {
        public static void ApplyPatch(Harmony harmony)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(Tree), nameof(Tree.draw)),
                postfix: new HarmonyMethod(typeof(TreeDrawPatch), nameof(Postfix))
            );
        }

        /// <summary>
        /// Postfix to add custom drawing logic for trees.
        /// </summary>
        /// <param name="__instance">The tree instance being drawn.</param>
        /// <param name="spriteBatch">SpriteBatch for rendering.</param>
        public static void Postfix(Tree __instance, SpriteBatch spriteBatch)
        {
            // Check if the tree has an associated owner
            if (!__instance.modData.TryGetValue(ModConstants.TreeOwnerKey, out var playerIdString))
                return;

            var farmer = Game1.getAllFarmers()
                .FirstOrDefault(f => f.UniqueMultiplayerID.ToString() == playerIdString);

            if (farmer == null)
                return;

            if (ModEntry.Config.IsShowTreeOwnerMiniPortrait)
                DrawFarmerPortrait(__instance, spriteBatch, farmer);

            if (IsMouseHoveringOverTree(__instance))
            {
                if (!ModEntry.Config.IsShowTreeOwnerMiniPortrait)
                    DrawFarmerPortrait(__instance, spriteBatch, farmer);

                DrawTooltip(spriteBatch, farmer);
            }
        }

        /// <summary>
        /// Draws the farmer's portrait next to the tree in the game world.
        /// </summary>
        /// <param name="tree">The tree object whose position is used to draw the portrait next to it.</param>
        /// <param name="spriteBatch">The SpriteBatch used for drawing the portrait to the screen.</param>
        /// <param name="farmer">The farmer whose portrait is drawn next to the tree. Represents the player owning the tree.</param>
        private static void DrawFarmerPortrait(Tree tree, SpriteBatch spriteBatch, Farmer farmer)
        {
            var treePosition = tree.Tile * 64; // Calculate the tree's position in the game world
            var localTreePosition = Game1.GlobalToLocal(Game1.viewport, treePosition); // Convert to screen space
            var portraitPosition = new Vector2(localTreePosition.X + 23.1f, localTreePosition.Y);

            var layerDepth = (tree.getBoundingBox().Bottom + 2) / 10000f;

            farmer.FarmerRenderer.drawMiniPortrat(
                spriteBatch,
                portraitPosition,
                layerDepth,
                scale: 1.5f,
                facingDirection: 2,
                who: farmer,
                alpha: 0.8f
            );
        }

        /// <summary>
        /// Checks if the mouse cursor is hovering over the given tree, based on its bounding box.
        /// </summary>
        /// <param name="tree">The tree object to check for mouse hover.</param>
        /// <returns>
        /// Returns <c>true</c> if the mouse is currently hovering over the tree, otherwise <c>false</c>.
        /// </returns>
        private static bool IsMouseHoveringOverTree(Tree tree)
        {
            var mouseX = Game1.getMouseX() + Game1.viewport.X;
            var mouseY = Game1.getMouseY() + Game1.viewport.Y;
            return tree.getBoundingBox().Contains(mouseX, mouseY) && Game1.activeClickableMenu == null;
        }

        /// <summary>
        /// Draws a tooltip near the mouse cursor displaying the tree owner's name.
        /// </summary>
        /// <param name="spriteBatch">The SpriteBatch used for drawing the tooltip text.</param>
        /// <param name="farmer">The farmer whose name will be displayed in the tooltip.</param>
        private static void DrawTooltip(SpriteBatch spriteBatch, Farmer farmer)
        {
            var message = ModEntry.StaticHelper.Translation.Get(
                ModConstants.TranslationKeys.YourTreeHint);

            if (farmer.UniqueMultiplayerID != Game1.player.UniqueMultiplayerID)
            {
                message = ModEntry.StaticHelper.Translation.Get(
                    ModConstants.TranslationKeys.TreeOwnerHint,
                    new { playerName = farmer.Name }
                );
            }

            var textSize = Game1.smallFont.MeasureString(message);
            const int padding = 16;

            var width = (int)(textSize.X + padding * 2);
            var height = (int)(textSize.Y + padding + 8);

            var boxX = Game1.getMouseX() + 32;
            var boxY = Game1.getMouseY() + 32;

            if (Game1.player.ActiveObject != null)
            {
                boxX += (int)(40 / Game1.options.desiredBaseZoomLevel);
            }

            IClickableMenu.drawTextureBox(
                spriteBatch,
                Game1.menuTexture,
                new Rectangle(0, 256, 60, 60),
                boxX,
                boxY,
                width,
                height,
                Color.White,
                1f,
                false
            );

            Utility.drawTextWithShadow(
                spriteBatch,
                message,
                Game1.smallFont,
                new Vector2(boxX + padding - 1, boxY + padding / 2 + 8),
                Game1.textColor,
                layerDepth: 1f,
                shadowIntensity: 0.5f
            );
        }
    }
}