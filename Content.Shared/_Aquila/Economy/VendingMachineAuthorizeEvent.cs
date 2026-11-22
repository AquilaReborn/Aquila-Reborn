namespace Content.Shared._Aquila.Economy;

/// <summary>
/// Событие проверки, может ли игрок пользоваться платным торгоматом без оплаты. Обработчик может выставить Authorized.
/// </summary>
[ByRefEvent]
public record struct VendingMachineAuthorizeEvent(EntityUid User, bool Authorized = false);
