using Content.Server.Aquila.Guardian;
using Content.Server.Guardian;
using Content.Server.Popups;
using Content.Shared.Aquila.HolyCorpse;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Random;

namespace Content.Server.Aquila.HolyCorpse;

public sealed class HolyCorpseSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly GuardianSystem _guardian = default!;
    [Dependency] private readonly HolyCorpseEmpowerSystem _empower = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyCorpseLimbComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HolyCorpseLimbComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HolyCorpseLimbComponent, HolyCorpseImplantDoAfterEvent>(OnImplantDoAfter);

        SubscribeLocalEvent<HolyCorpseHostComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HolyCorpseHostComponent, HolyCorpseExtractDoAfterEvent>(OnExtractDoAfter);
    }

    private void OnUseInHand(Entity<HolyCorpseLimbComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartImplant(ent.AsNullable(), args.User, args.User);
    }

    private void OnAfterInteract(Entity<HolyCorpseLimbComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target || !args.CanReach)
            return;

        args.Handled = true;
        TryStartImplant(ent.AsNullable(), args.User, target);
    }

    /// <summary>
    /// Пытается начать вживление конечности в цель.
    /// </summary>
    public bool TryStartImplant(Entity<HolyCorpseLimbComponent?> limb, EntityUid user, EntityUid target)
    {
        if (!Resolve(limb, ref limb.Comp) || !CanImplant(limb, user, target))
            return false;

        var flash = Spawn(limb.Comp.FlashProto, Transform(target).Coordinates);
        _transform.SetParent(flash, target);

        var ev = new HolyCorpseImplantDoAfterEvent(GetNetEntity(flash));
        var doAfterArgs = new DoAfterArgs(EntityManager, user, limb.Comp.ImplantDelay, ev, limb.Owner, target: target, used: limb.Owner)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfterArgs))
            return true;

        QueueDel(flash);
        return false;
    }

    /// <summary>
    /// Проверяет, можно ли вживить конечность в цель.
    /// </summary>
    public bool CanImplant(EntityUid limb, EntityUid user, EntityUid target, bool quiet = false)
    {
        if (!HasComp<MobStateComponent>(target) || HasComp<GuardianComponent>(target))
            return false;

        if (!HasSameLimb(target, limb))
            return true;

        if (!quiet)
            _popup.PopupEntity(Loc.GetString("holy-corpse-already-has-limb"), user, user);

        return false;
    }

    private bool HasSameLimb(EntityUid target, EntityUid limb)
    {
        if (!TryComp<HolyCorpseHostComponent>(target, out var host) || Prototype(limb)?.ID is not { } protoId)
            return false;

        foreach (var implanted in host.Limbs)
        {
            if (Prototype(implanted)?.ID == protoId)
                return true;
        }

        return false;
    }

    private void OnImplantDoAfter(Entity<HolyCorpseLimbComponent> ent, ref HolyCorpseImplantDoAfterEvent args)
    {
        if (args.FlashEntity is { } flash)
            QueueDel(GetEntity(flash));

        if (args.Handled || args.Cancelled || args.Args.Target is not { } target)
            return;

        if (!CanImplant(ent, args.Args.User, target))
            return;

        args.Handled = Implant(ent, target);
    }

    private bool Implant(Entity<HolyCorpseLimbComponent> limb, EntityUid target)
    {
        var host = EnsureComp<HolyCorpseHostComponent>(target);
        host.LimbContainer = _container.EnsureContainer<Container>(target, host.ContainerId);

        if (host.Limbs.Count == 0 && !TryApplySet(limb.Comp, (target, host)))
        {
            RemComp<HolyCorpseHostComponent>(target);
            return false;
        }

        _container.Insert(limb.Owner, host.LimbContainer);
        host.Limbs.Add(limb);

        UpdateGuardianStats((target, host));
        return true;
    }

    private bool TryApplySet(HolyCorpseLimbComponent limb, Entity<HolyCorpseHostComponent> host)
    {
        var set = _random.Pick(limb.Sets);
        if (_guardian.CreateGuardian(host, set.GuardianProto) is not { } guardian)
            return false;

        EnsureComp<GuardianPersistOnDeathComponent>(guardian);

        if (set.Components != null)
        {
            EntityManager.AddComponents(host, set.Components);
            host.Comp.AppliedComponents = set.Components;
        }

        if (TryComp<GuardianComponent>(guardian, out var guardianComp))
            host.Comp.BaseDistanceAllowed = guardianComp.DistanceAllowed;

        if (TryComp<RechargeBasicEntityAmmoComponent>(guardian, out var recharge))
            host.Comp.BaseRechargeCooldown = recharge.RechargeCooldown;

        return true;
    }

    private void UpdateGuardianStats(Entity<HolyCorpseHostComponent> host)
    {
        if (!TryComp<GuardianHostComponent>(host, out var guardianHost) || guardianHost.HostedGuardian is not { } guardian)
            return;

        var multiplier = 1f + host.Comp.BonusPerExtraLimb * Math.Max(0, host.Comp.Limbs.Count - 1);
        _empower.SetMultiplier(guardian, multiplier);

        if (host.Comp.BaseDistanceAllowed is { } distance && TryComp<GuardianComponent>(guardian, out var guardianComp))
            guardianComp.DistanceAllowed = distance * multiplier;

        if (host.Comp.BaseRechargeCooldown is { } cooldown && TryComp<RechargeBasicEntityAmmoComponent>(guardian, out var recharge))
        {
            recharge.RechargeCooldown = cooldown / multiplier;
            Dirty(guardian, recharge);
        }
    }

    private void OnGetVerbs(Entity<HolyCorpseHostComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<HolyCorpseHostComponent>(args.User, out var userHost) || userHost.Limbs.Count == 0)
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("holy-corpse-extract-verb"),
            Act = () => TryStartExtract(ent.AsNullable(), user),
        });
    }

    /// <summary>
    /// Пытается начать извлечение всех конечностей из носителя.
    /// </summary>
    public bool TryStartExtract(Entity<HolyCorpseHostComponent?> host, EntityUid user)
    {
        if (!Resolve(host, ref host.Comp))
            return false;

        var doAfterArgs = new DoAfterArgs(EntityManager, user, host.Comp.ExtractDelay, new HolyCorpseExtractDoAfterEvent(), host.Owner, target: host.Owner)
        {
            BreakOnMove = true,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnExtractDoAfter(Entity<HolyCorpseHostComponent> ent, ref HolyCorpseExtractDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        Extract(ent);
        args.Handled = true;
    }

    private void Extract(Entity<HolyCorpseHostComponent> host)
    {
        var coords = Transform(host).Coordinates;

        foreach (var limb in host.Comp.Limbs)
        {
            if (TerminatingOrDeleted(limb))
                continue;

            _container.Remove(limb, host.Comp.LimbContainer);
            _transform.SetCoordinates(limb, coords);
        }

        host.Comp.Limbs.Clear();

        if (TryComp<GuardianHostComponent>(host, out var guardianHost))
        {
            if (guardianHost.HostedGuardian is { } guardian && !TerminatingOrDeleted(guardian))
                Del(guardian);

            RemComp<GuardianHostComponent>(host);
        }

        if (host.Comp.AppliedComponents != null)
            EntityManager.RemoveComponents(host, host.Comp.AppliedComponents);

        RemComp<HolyCorpseHostComponent>(host);
    }
}
