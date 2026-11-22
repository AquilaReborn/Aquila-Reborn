namespace Content.Server._Aquila.Economy.Events;

/// <summary>
/// Событие неудачной попытки ввода PIN банковского счёта. Поднимается на сущности счёта.
/// </summary>
[ByRefEvent]
public readonly record struct BankPinFailedEvent(EntityUid Account);
