using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Kitchen.Events;

/// <summary>
/// Событие завершения затаскивания существа в мясорубку.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class MeatGrinderLoadDoAfterEvent : SimpleDoAfterEvent;
