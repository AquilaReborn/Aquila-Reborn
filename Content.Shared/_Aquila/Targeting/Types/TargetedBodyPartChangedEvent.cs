using Content.Shared._Shitmed.Targeting;

namespace Content.Shared._Aquila.Targeting.Types;

/// <summary>
///     Raised on a user when the body part selected on the targeting doll changes.
/// </summary>
[ByRefEvent]
public readonly record struct TargetedBodyPartChangedEvent(TargetBodyPart Part);
