using System.Numerics;
using Content.Shared._Aquila.Heretic.Lock.Components;
using Content.Shared._Shitcode.Heretic.Systems;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Throwing;
using Robust.Shared.Physics.Events;

namespace Content.Server._Aquila.Heretic.Lock.Systems;

/// <summary>
/// Отталкивает от барьера лабиринта всех, кто не служит Лабиринту.
/// </summary>
public sealed class LabyrinthBarrierSystem : EntitySystem
{
    [Dependency] private readonly SharedTransformSystem _xform = default!;
    [Dependency] private readonly SharedHereticSystem _heretic = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ThrowingSystem _throwing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LabyrinthBarrierComponent, StartCollideEvent>(OnStartCollide);
    }

    private void OnStartCollide(Entity<LabyrinthBarrierComponent> ent, ref StartCollideEvent args)
    {
        var other = args.OtherEntity;

        if (other == ent.Owner || TerminatingOrDeleted(other))
            return;

        if (!HasComp<MobStateComponent>(other))
            return;

        if (_heretic.IsHereticOrGhoul(other))
            return;

        var direction = _xform.GetWorldPosition(other) - _xform.GetWorldPosition(ent);

        if (direction == Vector2.Zero)
            direction = Transform(ent).LocalRotation.ToWorldVec();

        _throwing.TryThrow(other, direction, ent.Comp.ThrowForce, predicted: false);

        _popup.PopupEntity(Loc.GetString(ent.Comp.RepelMessage), other, other, PopupType.MediumCaution);
    }
}
