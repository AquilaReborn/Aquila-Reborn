using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.ArmorPlate;

[Serializable, NetSerializable]
public sealed partial class InsertArmorPlateDoAfterEvent : SimpleDoAfterEvent;
