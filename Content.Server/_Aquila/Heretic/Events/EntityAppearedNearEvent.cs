namespace Content.Server._Aquila.Heretic.Events;

/// <summary>
/// Поднимается на игроке, когда рядом с ним появляется сущность.
/// </summary>
[ByRefEvent]
public readonly record struct EntityAppearedNearEvent(EntityUid Entity, EntityUid Player);
