using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared._Aquila.Changeling;

public sealed partial class ChangelingTrueFormActionEvent : InstantActionEvent;

public sealed partial class ChangelingTrueFormRevertActionEvent : InstantActionEvent;

public sealed partial class ChangelingTrueFormRoarActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class ChangelingTrueFormDoAfterEvent : SimpleDoAfterEvent;
