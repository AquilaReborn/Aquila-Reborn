using Content.Shared.Chemistry.Components;

namespace Content.Shared.Chemistry.EntitySystems;

public sealed partial class SolutionRegenerationSystem
{
    private TimeSpan GetNextRegenTime(SolutionRegenerationComponent regen)
    {
        var next = regen.NextRegenTime + regen.Duration;
        return next > _timing.CurTime ? next : _timing.CurTime + regen.Duration;
    }
}
