using Content.Server.Guardian;
using Content.Server.Aquila.Guardian;
using Content.Server.Popups;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.DoAfter;
using Content.Shared.Guardian;
using Content.Shared.Aquila.HolyCorpse;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Verbs;
using Content.Shared.Weapons.Melee;
using Content.Shared.Weapons.Ranged.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Content.Shared.Mobs.Components;

namespace Content.Server.Aquila.HolyCorpse;

public sealed class HolyCorpseSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly GuardianSystem _guardian = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    private const float ExtractDelay = 7f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HolyCorpseLimbComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<HolyCorpseLimbComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<HolyCorpseLimbComponent, HolyCorpseImplantDoAfterEvent>(OnImplantDoAfter);

        SubscribeLocalEvent<HolyCorpseHostComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<HolyCorpseHostComponent, HolyCorpseExtractDoAfterEvent>(OnExtractDoAfter);
    }

    #region Implant

    private void OnUseInHand(EntityUid uid, HolyCorpseLimbComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        TryStartImplant(args.User, args.User, uid, component);
    }

    private void OnAfterInteract(EntityUid uid, HolyCorpseLimbComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target == null || !args.CanReach)
            return;

        args.Handled = true;
        TryStartImplant(args.User, args.Target.Value, uid, component);
    }

    private void TryStartImplant(EntityUid user, EntityUid target, EntityUid limb, HolyCorpseLimbComponent component)
    {
        if (!HasComp<MobStateComponent>(target))
        {
            return;
        }

        if (HasComp<GuardianComponent>(target))
        {
            return;
        }

        var protoId = Prototype(limb)?.ID;
        if (protoId == null)
            return;

        if (TryComp<HolyCorpseHostComponent>(target, out var existingHost))
        {
            foreach (var implanted in existingHost.Limbs)
            {
                if (implanted.ProtoId == protoId)
                {
                    _popup.PopupEntity(Loc.GetString("holy-corpse-already-has-limb"), user, user);
                    return;
                }
            }
        }

        var flash = Spawn(component.FlashProto, Transform(target).Coordinates);
        _transform.SetParent(flash, target);

        var ev = new HolyCorpseImplantDoAfterEvent(GetNetEntity(flash));
        var doAfterArgs = new DoAfterArgs(EntityManager, user, component.ImplantDelay, ev, limb, target: target, used: limb)
        {
            BreakOnMove = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            QueueDel(flash);
    }

    private void OnImplantDoAfter(EntityUid uid, HolyCorpseLimbComponent component, HolyCorpseImplantDoAfterEvent args)
    {
        if (args.FlashEntity != null)
            QueueDel(GetEntity(args.FlashEntity.Value));

        if (args.Handled || args.Cancelled || args.Args.Target == null || args.Args.Used == null)
            return;

        var target = args.Args.Target.Value;
        var limb = args.Args.Used.Value;

        if (component.Deleted || Deleted(limb))
            return;

        var protoId = Prototype(limb)?.ID;
        if (protoId == null)
            return;

        var host = EnsureComp<HolyCorpseHostComponent>(target);
        host.LimbContainer = _container.EnsureContainer<Container>(target, "HolyCorpseLimbContainer");

        if (!host.SetApplied)
        {
            var set = _random.Pick(component.Sets);
            var guardian = _guardian.CreateGuardian(target, set.GuardianProto);
            if (guardian == null)
            {
                RemComp<HolyCorpseHostComponent>(target);
                return;
            }

            EnsureComp<GuardianPersistOnDeathComponent>(guardian.Value);

            if (set.Components != null)
            {
                EntityManager.AddComponents(target, set.Components);
                host.AppliedComponents = set.Components;
            }

            CaptureBaseStats(guardian.Value, host);
            host.SetApplied = true;
        }

        _container.Insert(limb, host.LimbContainer);
        host.Limbs.Add((limb, protoId));

        RecalculateGuardianStats(target, host);

        args.Handled = true;
    }

    #endregion

    #region Extract

    private void OnGetVerbs(EntityUid uid, HolyCorpseHostComponent component, GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (!TryComp<HolyCorpseHostComponent>(args.User, out var userHost) || userHost.Limbs.Count == 0)
            return;

        var user = args.User;
        var target = uid;

        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("holy-corpse-extract-verb"),
            Act = () => StartExtract(user, target),
        });
    }

    private void StartExtract(EntityUid user, EntityUid target)
    {
        var doAfterArgs = new DoAfterArgs(EntityManager, user, ExtractDelay, new HolyCorpseExtractDoAfterEvent(), target, target: target)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnExtractDoAfter(EntityUid uid, HolyCorpseHostComponent component, HolyCorpseExtractDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Args.Target == null)
            return;

        var target = args.Args.Target.Value;
        if (!TryComp<HolyCorpseHostComponent>(target, out var host))
            return;

        var coords = Transform(target).Coordinates;

        foreach (var (limbEntity, _) in host.Limbs)
        {
            if (Deleted(limbEntity))
                continue;

            _container.Remove(limbEntity, host.LimbContainer);
            _transform.SetCoordinates(limbEntity, coords);
        }

        host.Limbs.Clear();

        if (TryComp<GuardianHostComponent>(target, out var guardianHost))
        {
            if (guardianHost.HostedGuardian != null && !Deleted(guardianHost.HostedGuardian.Value))
                Del(guardianHost.HostedGuardian.Value);

            RemComp<GuardianHostComponent>(target);
        }

        if (host.AppliedComponents != null)
            EntityManager.RemoveComponents(target, host.AppliedComponents);

        RemComp<HolyCorpseHostComponent>(target);

        args.Handled = true;
    }

    #endregion

    #region Stats

    private void CaptureBaseStats(EntityUid guardian, HolyCorpseHostComponent host)
    {
        if (TryComp<MeleeWeaponComponent>(guardian, out var melee))
        {
            host.BaseAttackRate = melee.AttackRate;
            host.BaseMeleeDamage = new DamageSpecifier(melee.Damage);
        }

        if (TryComp<StaminaDamageOnHitComponent>(guardian, out var stamina))
            host.BaseStaminaDamage = stamina.Damage;

        if (TryComp<GuardianComponent>(guardian, out var guardianComp))
            host.BaseDistanceAllowed = guardianComp.DistanceAllowed;

        if (TryComp<GunComponent>(guardian, out var gun))
            host.BaseFireRate = gun.FireRateModified;

        if (TryComp<RechargeBasicEntityAmmoComponent>(guardian, out var recharge))
            host.BaseRechargeCooldown = recharge.RechargeCooldown;
    }

    private void RecalculateGuardianStats(EntityUid hostUid, HolyCorpseHostComponent host)
    {
        if (!TryComp<GuardianHostComponent>(hostUid, out var guardianHost) || guardianHost.HostedGuardian is not { } guardian)
            return;

        var count = host.Limbs.Count;
        var multiplier = count > 1 ? 1f + 0.10f * (count - 1) : 1f;

        if (host.BaseAttackRate != null && TryComp<MeleeWeaponComponent>(guardian, out var melee))
        {
            melee.AttackRate = host.BaseAttackRate.Value * multiplier;
            if (host.BaseMeleeDamage != null)
                melee.Damage = host.BaseMeleeDamage * multiplier;
            Dirty(guardian, melee);
        }

        if (host.BaseStaminaDamage != null && TryComp<StaminaDamageOnHitComponent>(guardian, out var stamina))
        {
            stamina.Damage = host.BaseStaminaDamage.Value * multiplier;
        }

        if (host.BaseDistanceAllowed != null && TryComp<GuardianComponent>(guardian, out var guardianComp))
        {
            guardianComp.DistanceAllowed = host.BaseDistanceAllowed.Value * multiplier;
        }

        if (host.BaseFireRate != null && TryComp<GunComponent>(guardian, out var gun))
        {
            gun.FireRateModified = host.BaseFireRate.Value * multiplier;
            Dirty(guardian, gun);
        }

        if (host.BaseRechargeCooldown != null && TryComp<RechargeBasicEntityAmmoComponent>(guardian, out var recharge))
        {
            recharge.RechargeCooldown = host.BaseRechargeCooldown.Value / multiplier;
            Dirty(guardian, recharge);
        }
    }

    #endregion
}
