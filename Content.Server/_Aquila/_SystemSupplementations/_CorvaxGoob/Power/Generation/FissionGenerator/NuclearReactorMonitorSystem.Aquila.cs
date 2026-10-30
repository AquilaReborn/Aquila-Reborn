using Content.Shared.ActionBlocker;

namespace Content.Server._CorvaxGoob.Power.Generation.FissionGenerator;

public sealed partial class NuclearReactorMonitorSystem
{
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
}
