using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Systems;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Лунный амулет обращает достаточно обезумевшего члена экипажа в союзника еретика пути Луны.
/// </summary>
public sealed class MoonAmuletSystem : SharedMoonAmuletSystem
{
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly MoonAscensionSystem _ascension = default!;

    private static readonly LocId ConversionFailedMessage = "moon-amulet-conversion-failed";

    /// <summary>
    /// Подключает применение удерживаемого амулета к цели.
    /// </summary>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MoonAmuletComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(Entity<MoonAmuletComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = TryConvertWithAmulet((ent.Owner, ent.Comp), args.User, target);
    }

    /// <summary>
    /// Обращает подходящую цель амулетом и расходует кулдаун только после успешной выдачи роли.
    /// </summary>
    public bool TryConvertWithAmulet(Entity<MoonAmuletComponent?> ent, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (!Resolve(ent.Owner, ref ent.Comp))
            return false;

        if (!CanConvertWithAmulet((ent.Owner, ent.Comp), user, target, quiet))
            return false;

        if (DoConvertWithAmulet((ent.Owner, ent.Comp), user, target))
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(ConversionFailedMessage), ent.Owner, user);
        return false;
    }

    private bool CanConvertWithAmulet(Entity<MoonAmuletComponent> ent, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (user != target && !TerminatingOrDeleted(ent.Owner) && !EntityManager.IsQueuedForDeletion(ent.Owner) &&
            !TerminatingOrDeleted(user) && !TerminatingOrDeleted(target) &&
            _timing.CurTime >= ent.Comp.NextUse && _hands.IsHolding(user, ent.Owner) &&
            _interaction.InRangeUnobstructed(user, target) &&
            _heretic.TryGetHereticComponent(user, out var heretic, out _) && heretic.CurrentPath == ent.Comp.RequiredPath)
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString(ConversionFailedMessage), ent.Owner, user);
        return false;
    }

    private bool DoConvertWithAmulet(Entity<MoonAmuletComponent> ent, EntityUid user, EntityUid target)
    {
        if (!_ascension.TryConvertLunatic((target, null), user, ent.Comp.ConversionThreshold,
                ent.Comp.Effect, ent.Comp.LunaticAmulet, ent.Comp.LaughSound))
            return false;

        ent.Comp.NextUse = _timing.CurTime + ent.Comp.UseDelay;
        return true;
    }
}
