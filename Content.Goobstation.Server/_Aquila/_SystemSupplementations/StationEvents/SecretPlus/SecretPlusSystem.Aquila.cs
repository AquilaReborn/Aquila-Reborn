using Content.Shared.GameTicking.Components;

namespace Content.Goobstation.Server.StationEvents.SecretPlus;

public sealed partial class SecretPlusSystem
{
    private void AllowSkipOnTooFewPlayers(EntityUid ruleUid, bool doStart)
    {
        if (doStart || !TryComp<GameRuleComponent>(ruleUid, out var ruleComp))
            return;

        ruleComp.CancelPresetOnTooFewPlayers = false;
    }
}
