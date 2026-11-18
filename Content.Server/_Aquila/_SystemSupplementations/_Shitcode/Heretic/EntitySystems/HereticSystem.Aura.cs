using Content.Server._Aquila.Heretic;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticSystem
{
    [Dependency] private readonly HereticAuraSystem _aura = default!;
}
