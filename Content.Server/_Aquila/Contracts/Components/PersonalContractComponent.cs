using Content.Server._Aquila.Contracts.DataDefinitions;
using Content.Server._Aquila.Contracts.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Contracts.Components;

/// <summary>
/// Хранит личные контракты того, кто выдаёт работу: какая работа закреплена за каким счётом,
/// на сколько она закреплена и какая премия за неё положена.
/// </summary>
[RegisterComponent, Access(typeof(PersonalContractSystem))]
public sealed partial class PersonalContractComponent : Component
{
    /// <summary>
    /// Взятые работы, сопоставленные контракту счёта, который их взял. Ключ - идентификатор работы.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<CargoBountyPrototype>, PersonalContractData> Contracts = new();

    /// <summary>
    /// Сколько работ один счёт может держать одновременно. Просроченные работы слот не занимают.
    /// </summary>
    [DataField]
    public int MaxActiveContracts = 2;

    /// <summary>
    /// Сколько времени работа остаётся закреплённой за счётом, прежде чем её сможет взять кто угодно.
    /// </summary>
    [DataField]
    public TimeSpan ClaimDuration = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Доля награды за работу, которая уходит на счёт как личная премия.
    /// </summary>
    [DataField]
    public float PremiumShare = 0.02f;

    /// <summary>
    /// Наибольшая личная премия за одну работу.
    /// </summary>
    [DataField]
    public int MaxPremium = 200;
}
