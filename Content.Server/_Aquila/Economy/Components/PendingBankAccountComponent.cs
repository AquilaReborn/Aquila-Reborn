using Content.Server._Aquila.Economy.Systems;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Отложенное открытие банковского счёта: на момент спавна на мобе ещё нет ID-карты, поэтому счёт открывается позже.
/// </summary>
[RegisterComponent, Access(typeof(BankAccountSystem))]
public sealed partial class PendingBankAccountComponent : Component
{
    /// <summary>
    /// Должность владельца, по которой считается зарплата, если на карте её ещё нет.
    /// </summary>
    [ViewVariables]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Имя владельца счёта.
    /// </summary>
    [ViewVariables]
    public string Name = string.Empty;

    /// <summary>
    /// Когда отложенное открытие счёта перестаёт быть нужным.
    /// </summary>
    [ViewVariables]
    public TimeSpan Expiry;
}
