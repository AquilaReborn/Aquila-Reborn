using System.Linq;
using System.Numerics;
using Content.Goobstation.Common.Actions;
using Content.Goobstation.Shared._Aquila.Changeling;
using Content.Goobstation.Shared.Changeling.Components;
using Content.Server.Polymorph.Systems;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Camera;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs.Components;
using Content.Shared.Polymorph;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Goobstation.Server._Aquila.Changeling;

public sealed class ChangelingTrueFormSystem : EntitySystem
{
    private static readonly ProtoId<PolymorphPrototype> TrueFormPolymorph = "ChangelingTrueForm";

    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly PolymorphSystem _polymorph = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedCameraRecoilSystem _recoil = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingTrueFormActionEvent>(OnTrueFormAction);
        SubscribeLocalEvent<ChangelingIdentityComponent, ChangelingTrueFormDoAfterEvent>(OnTrueFormDoAfter);
        SubscribeLocalEvent<ChangelingTrueFormComponent, ChangelingTrueFormRevertActionEvent>(OnRevertAction);
        SubscribeLocalEvent<ChangelingTrueFormComponent, ChangelingTrueFormRoarActionEvent>(OnRoarAction);
        SubscribeLocalEvent<ChangelingTrueFormComponent, PolymorphedEvent>(OnTrueFormPolymorphed);
        SubscribeLocalEvent<ChangelingTrueFormChargeComponent, PolymorphedEvent>(OnChargePolymorphed);
    }

    private void OnTrueFormAction(Entity<ChangelingIdentityComponent> ent, ref ChangelingTrueFormActionEvent args)
    {
        if (args.Handled)
            return;

        var charge = EnsureComp<ChangelingTrueFormChargeComponent>(ent);
        if (charge.AbsorbedAtLastUse >= 0 && ent.Comp.TotalAbsorbedEntities <= charge.AbsorbedAtLastUse)
        {
            _popup.PopupEntity(Loc.GetString("changeling-trueform-no-charge"), ent, ent);
            return;
        }

        var doAfter = new DoAfterArgs(EntityManager, ent, charge.TransformDelay, new ChangelingTrueFormDoAfterEvent(), ent)
        {
            BreakOnDamage = false,
            BreakOnMove = false,
            MultiplyDelay = false,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _popup.PopupEntity(Loc.GetString("changeling-trueform-start", ("user", Identity.Entity(ent, EntityManager))), ent, PopupType.LargeCaution);
        _audio.PlayPvs(charge.TransformSound, ent);

        args.Handled = true;
    }

    private void OnTrueFormDoAfter(Entity<ChangelingIdentityComponent> ent, ref ChangelingTrueFormDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var charge = EnsureComp<ChangelingTrueFormChargeComponent>(ent);
        var previousUse = charge.AbsorbedAtLastUse;
        charge.AbsorbedAtLastUse = ent.Comp.TotalAbsorbedEntities;

        if (_polymorph.PolymorphEntity(ent, TrueFormPolymorph) is not { } form)
        {
            charge.AbsorbedAtLastUse = previousUse;
            return;
        }

        HideChangelingActions(form);
        RaiseNetworkEvent(new LoadActionsEvent(GetNetEntity(ent)), form);

        if (!TryComp<ChangelingTrueFormComponent>(form, out var trueForm))
            return;

        Roar((form, trueForm));
    }

    private void OnRoarAction(Entity<ChangelingTrueFormComponent> ent, ref ChangelingTrueFormRoarActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        Roar(ent);
    }

    private void Roar(Entity<ChangelingTrueFormComponent> ent)
    {
        _audio.PlayPvs(ent.Comp.RoarSound, ent);
        SpawnAttachedTo(ent.Comp.RoarEffect, new EntityCoordinates(ent, 0f, 0f));
        DoRoarWave(ent);
    }

    private void HideChangelingActions(EntityUid form)
    {
        foreach (var action in _actions.GetActions(form).ToList())
        {
            if (HasComp<ChangelingActionComponent>(action))
                _actions.RemoveAction(form, new Entity<ActionComponent?>(action.Owner, action.Comp));
        }
    }

    private void DoRoarWave(Entity<ChangelingTrueFormComponent> ent)
    {
        var center = _transform.GetMapCoordinates(ent);

        foreach (var target in _lookup.GetEntitiesInRange<MobStateComponent>(center, ent.Comp.WaveRange))
        {
            if (target.Owner == ent.Owner)
                continue;

            _stun.TryKnockdown(target.Owner, ent.Comp.WaveKnockdownDuration);

            var direction = _transform.GetMapCoordinates(target.Owner).Position - center.Position;
            if (direction.LengthSquared() < 0.01f)
                continue;

            _throwing.TryThrow(target.Owner, direction.Normalized() * ent.Comp.WaveThrowDistance, ent.Comp.WaveThrowSpeed);
        }

        foreach (var player in _lookup.GetEntitiesInRange<ActorComponent>(center, ent.Comp.ShakeRange))
        {
            var direction = _transform.GetMapCoordinates(player.Owner).Position - center.Position;
            if (direction.LengthSquared() < 0.01f)
                direction = Vector2.UnitX;

            _recoil.KickCamera(player.Owner, direction.Normalized());
        }
    }

    private void OnRevertAction(Entity<ChangelingTrueFormComponent> ent, ref ChangelingTrueFormRevertActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        _polymorph.Revert(ent.Owner);
    }

    private void OnTrueFormPolymorphed(Entity<ChangelingTrueFormComponent> ent, ref PolymorphedEvent args)
    {
        if (!args.IsRevert || args.OldEntity != ent.Owner)
            return;

        _stun.TryUpdateParalyzeDuration(args.NewEntity, ent.Comp.RevertStunDuration);
    }

    private void OnChargePolymorphed(Entity<ChangelingTrueFormChargeComponent> ent, ref PolymorphedEvent args)
    {
        _polymorph.CopyPolymorphComponent<ChangelingTrueFormChargeComponent>(ent, args.NewEntity);
    }
}
