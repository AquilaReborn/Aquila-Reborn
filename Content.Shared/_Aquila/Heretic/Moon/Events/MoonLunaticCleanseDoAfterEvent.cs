using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Heretic.Moon.Events;

[Serializable, NetSerializable]
public sealed partial class MoonLunaticCleanseDoAfterEvent : SimpleDoAfterEvent;
