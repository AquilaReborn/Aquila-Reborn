using Content.Shared._Aquila.Targeting.Types;
using Content.Shared._Shitmed.Targeting;
using Content.Shared._Shitmed.Targeting.Events;

namespace Content.Shared._Aquila.Targeting.Systems;

/// <summary>
///     Exposes the targeting doll selection to other systems: <see cref="TryGetTargetedBodyPart"/>
///     for the current body part and <see cref="TargetedBodyPartChangedEvent"/> for changes.
/// </summary>
public sealed class BodyPartTargetingSystem : EntitySystem
{
    /// <summary>
    ///     Gets the body part currently selected on the targeting doll of the given entity.
    /// </summary>
    public bool TryGetTargetedBodyPart(EntityUid user, out TargetBodyPart? part)
    {
        part = null;

        if (!TryComp<TargetingComponent>(user, out var targeting))
            return false;

        part = targeting.Target;
        return true;
    }

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<TargetChangeEvent>(OnTargetChange);
    }

    private void OnTargetChange(TargetChangeEvent message, EntitySessionEventArgs args)
    {
        var uid = GetEntity(message.Uid);

        if (!TryComp<TargetingComponent>(uid, out _))
            return;

        var ev = new TargetedBodyPartChangedEvent(message.BodyPart);
        RaiseLocalEvent(uid, ref ev);
    }
}
