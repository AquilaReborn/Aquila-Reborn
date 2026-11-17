using Content.Goobstation.Server.MartialArts;
using Content.Goobstation.Shared.MartialArts.Components;
using Content.Server.Heretic.Components;

namespace Content.Goobstation.Server._Aquila.MartialArts;

public sealed partial class SecurityJudoSystem : EntitySystem
{
    [Dependency] private MartialArtsSystem _martialArts = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<SecurityStaffComponent, ComponentStartup>(OnSecurityStaffStartup);
    }

    private void OnSecurityStaffStartup(Entity<SecurityStaffComponent> ent, ref ComponentStartup args)
    {
        EnsureComp<SystemGrantedJudoComponent>(ent.Owner);
        _martialArts.TryGrantCorporateJudo(ent.Owner);
    }
}
