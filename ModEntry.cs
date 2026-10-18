using HarmonyLib;
using PersonalTrees.Constants;
using PersonalTrees.Messages;
using PersonalTrees.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using UniversalPauseCommand.Config;

namespace PersonalTrees
{
    internal sealed class ModEntry : Mod
    {
        public static IModHelper StaticHelper = null!;
        public static IMonitor StaticMonitor = null!;
        public static ModConfig Config = null!;

        public override void Entry(IModHelper helper)
        {
            StaticHelper = helper;
            StaticMonitor = Monitor;

            var harmony = new Harmony(ModManifest.UniqueID);
            TreePlacementPatch.ApplyPatch(harmony);
            TreeToolActionPatch.ApplyPatch(harmony);
            TreeDrawPatch.ApplyPatch(harmony);

            Config = helper.ReadConfig<ModConfig>();
            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Multiplayer.PeerConnected += OnPeerConnected;
            helper.Events.Multiplayer.ModMessageReceived += MessageManager.HandleMessage;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            if (StaticHelper.ModRegistry.IsLoaded("spacechase0.GenericModConfigMenu"))
                SetupGenericModConfigMenu();
        }

        private void SetupGenericModConfigMenu()
        {
            var gmcmApi = StaticHelper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcmApi == null)
                return;

            gmcmApi.Register(
                mod: ModManifest,
                reset: () => Config = new ModConfig(),
                save: () => StaticHelper.WriteConfig(Config),
                titleScreenOnly: false
            );

            gmcmApi.AddKeybind(
                mod: ModManifest,
                name: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ToggleButtonName),
                tooltip: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ToggleButtonTooltip),
                getValue: () => Config.TreeOwnerMiniPortraitToggleButton,
                setValue: value => Config.TreeOwnerMiniPortraitToggleButton = value
            );

            gmcmApi.AddKeybind(
                mod: ModManifest,
                name: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.TreeOwnershipToggleButtonName),
                tooltip: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.TreeOwnershipToggleButtonTooltip),
                getValue: () => Config.TreeOwnershipToggleButton,
                setValue: value => Config.TreeOwnershipToggleButton = value
            );

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                name: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ShowOptionName),
                tooltip: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ShowOptionTooltip),
                getValue: () => Config.IsShowTreeOwnerMiniPortrait,
                setValue: value => Config.IsShowTreeOwnerMiniPortrait = value
            );
        }

        private void OnPeerConnected(object? sender, PeerConnectedEventArgs e)
        {
            if (!Game1.IsMasterGame)
                return;

            var peer = e.Peer;
            var peerName = Game1.getOnlineFarmers().ToList()
                .Find(farmer => farmer.UniqueMultiplayerID == peer.PlayerID)?.Name;
            var peerMod = peer.Mods.FirstOrDefault(mod => mod.ID == ModManifest.UniqueID);

            if (peerMod != null)
                return;

            var message = $"[{ModManifest.Name}] " +
                          StaticHelper.Translation.Get("message.noMod", new { name = peerName });
            MessageManager.SendGlobalInfoMessage(message);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == Config.TreeOwnershipToggleButton && Context.IsPlayerFree && Context.CanPlayerMove &&
                !Game1.player.UsingTool && Game1.chatBox?.isActive() != true &&
                !Game1.eventUp && Game1.currentLocation != null)
            {
                var cursorTile = e.Cursor.GrabTile;
                var playerTile = Game1.player.Tile;
                if (Math.Abs(cursorTile.X - playerTile.X) <= 1 && Math.Abs(cursorTile.Y - playerTile.Y) <= 1 &&
                    PersonalTrees.TreeOwnershipManager.TryToggleOwnership(Game1.currentLocation, cursorTile, Game1.player))
                    StaticHelper.Input.Suppress(e.Button);
            }

            if (e.Button == Config.TreeOwnerMiniPortraitToggleButton && Context.IsWorldReady)
            {
                Config.IsShowTreeOwnerMiniPortrait = !Config.IsShowTreeOwnerMiniPortrait;
                StaticHelper.WriteConfig(Config);
            }
        }
    }
}
