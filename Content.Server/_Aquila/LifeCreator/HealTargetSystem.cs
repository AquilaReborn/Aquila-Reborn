using Content.Shared.Aquila.LifeCreator;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Server.Popups;
using Content.Server.DoAfter;
using Content.Goobstation.Maths.FixedPoint;
using Content.Shared.Mobs.Systems;

namespace Content.Server.Aquila.LifeCreator;

public sealed class HealTargetSystem : EntitySystem
{
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HealTargetActionEvent>(OnAction);
        SubscribeLocalEvent<LifeCreatorComponent, HealTargetDoAfterEvent>(OnDoAfter);
    }

    private void OnAction(HealTargetActionEvent args)
    {
        if (args.Handled) return;

        var performer = args.Performer;

        if (!HasComp<LifeCreatorComponent>(performer))
            return;

        var target = args.Target;
        if (!Exists(target))
            return;

        args.Handled = true;

        _popup.PopupEntity(Loc.GetString("heal-target-start"), performer, performer);

        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            performer,
            TimeSpan.FromSeconds(3),
            new HealTargetDoAfterEvent(),
            performer,
            target: target)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnDoAfter(EntityUid performer, LifeCreatorComponent component, HealTargetDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled) return;
        args.Handled = true;

        if (args.Args.Target is not { } target || !Exists(target))
            return;

        if (!TryComp<DamageableComponent>(target, out var damageable))
            return;

        _mobThreshold.SetAllowRevives(target, true);
        _damageable.SetAllDamage(target, damageable, FixedPoint2.Zero);
        _mobThreshold.SetAllowRevives(target, false);
    }
}
