using Content.Shared._Aquila.CCVars;
using Robust.Shared.Configuration;

namespace Content.Goobstation.Shared.Changeling.Systems;

public abstract partial class SharedChanglingActionSystem
{
    [Dependency] private IConfigurationManager _cfg = default!;

    private bool IsAbsorbedRequirementEnabled()
    {
        return _cfg.GetCVar(AquilaCVars.ChangelingRequireAbsorbed);
    }
}
