using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Engram;

[Serializable, NetSerializable]
public sealed partial class SoulkillerExtractDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class EngramChipInsertDoAfterEvent : SimpleDoAfterEvent;
