using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Language;

[Serializable, NetSerializable]
public sealed partial class TranslatorChipInsertDoAfterEvent : SimpleDoAfterEvent;
