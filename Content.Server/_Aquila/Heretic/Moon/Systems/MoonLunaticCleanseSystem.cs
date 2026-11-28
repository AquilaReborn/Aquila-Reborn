using Content.Goobstation.Common.Religion;
using Content.Goobstation.Shared.Bible;
using Content.Server._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Снимает лунатизм с человека, когда священник взаимодействует с ним библией.
/// </summary>
public sealed class MoonLunaticCleanseSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MoonAscensionSystem _moonAscension = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MoonLunaticComponent, AfterInteractUsingEvent>(OnBibleInteract);
        SubscribeLocalEvent<MoonLunaticCleanseDoAfterEvent>(OnCleanseDoAfter);
    }

    private void OnBibleInteract(Entity<MoonLunaticComponent> ent, ref AfterInteractUsingEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        if (!HasComp<BibleComponent>(args.Used))
            return;

        if (!HasComp<BibleUserComponent>(args.User))
            return;

        if (!HasComp<MoonLunaticComponent>(target))
            return;

        if (!TryComp<MoonLunaticComponent>(target, out var lunatic))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, lunatic.CleanseDuration,
            new MoonLunaticCleanseDoAfterEvent(), args.User, target: target, used: args.Used)
        {
            NeedHand = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            Broadcast = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnCleanseDoAfter(MoonLunaticCleanseDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || args.Target is not { } target)
            return;

        if (!HasComp<MoonLunaticComponent>(target))
            return;

        _moonAscension.TryCleanseLunatic(target, args.User);
        args.Handled = true;
    }
}
