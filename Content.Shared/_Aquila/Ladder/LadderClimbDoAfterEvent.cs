using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Ladder;

/// <summary>
/// Завершение ожидания у лестницы. Поднимается на самой лестнице.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class LadderClimbDoAfterEvent : SimpleDoAfterEvent;
