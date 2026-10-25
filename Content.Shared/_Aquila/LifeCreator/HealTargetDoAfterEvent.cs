using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.LifeCreator;

[Serializable, NetSerializable]
public sealed partial class HealTargetDoAfterEvent : SimpleDoAfterEvent;
