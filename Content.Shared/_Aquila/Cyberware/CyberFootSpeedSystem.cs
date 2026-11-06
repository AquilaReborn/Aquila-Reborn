using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Shitmed.Body.Components;
using Content.Shared.Body.Components;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Movement.Systems;

namespace Content.Shared._Aquila.Cyberware;

/// <summary>
/// Кибернетические ступни немного ускоряют носителя.
/// </summary>
public sealed class CyberFootSpeedSystem : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly MovementSpeedModifierSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BodyComponent, RefreshMovementSpeedModifiersEvent>(OnRefreshSpeed);
        SubscribeLocalEvent<CyberFootSpeedComponent, BodyPartAddedEvent>(OnFootAdded);
        SubscribeLocalEvent<CyberFootSpeedComponent, BodyPartRemovedEvent>(OnFootRemoved);
    }

    private void OnRefreshSpeed(Entity<BodyComponent> ent, ref RefreshMovementSpeedModifiersEvent args)
    {
        foreach (var (foot, _) in _body.GetBodyChildrenOfType(ent, BodyPartType.Foot, ent.Comp))
        {
            if (!TryComp<CyberFootSpeedComponent>(foot, out var speed)
                || HasComp<LimbParalyzedComponent>(foot))
                continue;

            args.ModifySpeed(speed.WalkModifier, speed.SprintModifier);
        }
    }

    private void OnFootAdded(Entity<CyberFootSpeedComponent> ent, ref BodyPartAddedEvent args)
    {
        if (args.Part.Comp.Body is { } body)
            _movement.RefreshMovementSpeedModifiers(body);
    }

    private void OnFootRemoved(Entity<CyberFootSpeedComponent> ent, ref BodyPartRemovedEvent args)
    {
        if (args.Part.Comp.Body is { } body)
            _movement.RefreshMovementSpeedModifiers(body);
    }
}
