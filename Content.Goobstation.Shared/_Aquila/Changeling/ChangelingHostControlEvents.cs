using Content.Shared.Actions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared._Aquila.Changeling;

public sealed partial class ChangelingOfferHostControlActionEvent : InstantActionEvent;

public sealed partial class ChangelingReturnHostControlActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed class ChangelingHostControlRequestMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
