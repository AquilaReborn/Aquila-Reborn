using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Economy.Components;

/// <summary>
/// Настройки казны станции: откуда идут выплаты экипажу и с какой суммы сообщать о крупных операциях.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class StationTreasuryComponent : Component
{
    /// <summary>
    /// Порог крупной операции по умолчанию: с такой суммы о ней уведомляют службу безопасности.
    /// </summary>
    public const int DefaultLargeTransactionThreshold = 1000;

    /// <summary>
    /// Счёт казны, с которого идут личные выплаты экипажу.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> PayrollAccount = "Cargo";

    /// <summary>
    /// Платить ли зарплату из казны станции, а не выдавать её из воздуха.
    /// </summary>
    [DataField]
    public bool PaySalaryFromTreasury = true;

    /// <summary>
    /// Покрывать ли субсидией ЦК нехватку казны на зарплату: казна платит сколько есть, остаток доплачивает ЦК.
    /// </summary>
    [DataField]
    public bool CoverSalaryWithSubsidy = true;

    /// <summary>
    /// Платить ли подъёмные при открытии счёта из казны станции. По умолчанию нет: подъёмные субсидирует ЦК, чтобы казна не обнулялась на старте смены.
    /// </summary>
    [DataField]
    public bool PayStartingFromTreasury;

    /// <summary>
    /// Покрывать ли субсидией ЦК нехватку казны на подъёмные, если они идут из казны.
    /// </summary>
    [DataField]
    public bool CoverStartingWithSubsidy = true;

    /// <summary>
    /// Покрывать ли субсидией ЦК премии квот.
    /// </summary>
    [DataField]
    public bool CoverQuotaWithSubsidy;

    /// <summary>
    /// Сумма операции, начиная с которой о ней стоит уведомлять службу безопасности.
    /// </summary>
    [DataField]
    public int LargeTransactionThreshold = DefaultLargeTransactionThreshold;

    /// <summary>
    /// Причина субсидии ЦК в истории операций.
    /// </summary>
    [DataField]
    public LocId SubsidyReason = "bank-transaction-subsidy";
}
