using Content.Shared.VendingMachines;

namespace Content.Shared._Aquila.Economy;

/// <summary>
/// Отменяемая попытка покупки в платном торгомате. Обработчик может отменить покупку через Cancelled.
/// </summary>
[ByRefEvent]
public record struct VendingMachinePurchaseAttemptEvent(EntityUid User, string ItemId, InventoryType Type, bool Cancelled = false);
