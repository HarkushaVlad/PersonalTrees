using StardewModdingAPI;

namespace UniversalPauseCommand.Config;

internal class ModConfig
{
    public SButton TreeOwnerMiniPortraitToggleButton { get; set; } = SButton.J;
    public SButton TreeOwnershipToggleButton { get; set; } = SButton.K;
    public bool IsShowTreeOwnerMiniPortrait { get; set; } = true;
}
