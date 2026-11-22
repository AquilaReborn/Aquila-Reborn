using Content.Shared._Aquila.Economy.DataDefinitions;

namespace Content.Server._Aquila.Economy;

/// <summary>
/// Событие операции по банковскому счёту. Поднимается на сущности счёта и на ID-карте, привязанной к счёту.
/// </summary>
[ByRefEvent]
public readonly record struct BankTransactionEvent(EntityUid Account, BankTransaction Transaction);
