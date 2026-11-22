using Content.Server._Aquila.Economy.Systems;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Отмечает сущность, чей клиент вошёл в игру и готов принять PIN счёта: в этот момент попап не потеряется.
/// </summary>
[RegisterComponent, Access(typeof(BankAccountSystem))]
public sealed partial class BankPinReadyComponent : Component
{
}
