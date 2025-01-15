using HarmonyLib;
using PersonalTrees.Constants;
using PersonalTrees.Patches;
using StardewModdingAPI;
using StardewModdingAPI.Events;
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

            gmcmApi.AddBoolOption(
                mod: ModManifest,
                name: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ShowOptionName),
                tooltip: () => StaticHelper.Translation.Get(ModConstants.TranslationKeys.ShowOptionTooltip),
                getValue: () => Config.IsShowTreeOwnerMiniPortrait,
                setValue: value => Config.IsShowTreeOwnerMiniPortrait = value
            );
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (e.Button == Config.TreeOwnerMiniPortraitToggleButton && Context.IsWorldReady)
            {
                Config.IsShowTreeOwnerMiniPortrait = !Config.IsShowTreeOwnerMiniPortrait;
                StaticHelper.WriteConfig(Config);
            }
        }
    }
}