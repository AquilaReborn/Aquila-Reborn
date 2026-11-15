using Content.Shared.Actions;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.WOU.Stand;

[Serializable, NetSerializable]
public enum WOUVisuals : byte
{
    State
}

[Serializable, NetSerializable]
public enum WOUVisualState : byte
{
    ModeOne,
    ModeTwo
}

public sealed partial class WOUBeeSphereActionEvent : InstantActionEvent;
public sealed partial class WOUCapsuleSphereActionEvent : InstantActionEvent;
public sealed partial class WOUDoubleReflectActionEvent : InstantActionEvent;
public sealed partial class WOUEnterModeTwoActionEvent : InstantActionEvent;
public sealed partial class WOUSlipSphereActionEvent : InstantActionEvent;
public sealed partial class WOUToggleFormActionEvent : InstantActionEvent;