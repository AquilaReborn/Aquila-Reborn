using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy.Components;

[RegisterComponent, Access(typeof(SharedPaymentTerminalSystem))]
public sealed partial class PaymentTerminalComponent : Component
{
    /// <summary>
    /// Счёт, привязанный к терминалу. Оплаты покупателей уходят на него. Терминалу штрафов счёт не нужен:
    /// его оплаты уходят в <see cref="FineBudget"/>.
    /// </summary>
    [ViewVariables]
    public EntityUid? LinkedAccount;

    /// <summary>
    /// Сумма выставленного счёта в кредитах.
    /// </summary>
    [ViewVariables]
    public int Amount;

    /// <summary>
    /// Назначение выставленного счёта, которое покупатель увидит в истории операций.
    /// </summary>
    [ViewVariables]
    public string Purpose = string.Empty;

    /// <summary>
    /// Готов ли терминал принять оплату: счёт выставлен. Обычный терминал счёт не снимает и принимает оплату много раз,
    /// терминал штрафов снимает счёт после успешной оплаты.
    /// </summary>
    [ViewVariables]
    public bool Ready;

    /// <summary>
    /// Терминал выписывает только штрафы: оплата списывается с карты покупателя в бюджет станции, а не на счёт владельца терминала.
    /// Счёт терминалу не нужен, выставлять штраф может любой сотрудник с доступом к терминалу.
    /// </summary>
    [DataField]
    public bool FineOnly;

    /// <summary>
    /// Бюджет станции, куда идут штрафы.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> FineBudget = "Security";

    /// <summary>
    /// Когда каждый плательщик последний раз платил через этот терминал. Нужно для кулдауна оплаты.
    /// </summary>
    [ViewVariables]
    public Dictionary<EntityUid, TimeSpan> LastPayments = new();

    /// <summary>
    /// Последняя успешная оплата через терминал. Null, если платить ещё не приходили. Из неё печатается чек.
    /// </summary>
    [ViewVariables]
    public PaymentTerminalReceipt? LastReceipt;

    /// <summary>
    /// Описание оплаты в истории операций плательщика.
    /// </summary>
    [DataField]
    public LocId PaymentOutReason = "bank-transaction-terminal-payment-out";

    /// <summary>
    /// Описание оплаты в истории операций получателя.
    /// </summary>
    [DataField]
    public LocId PaymentInReason = "bank-transaction-terminal-payment-in";

    /// <summary>
    /// Описание штрафа в истории операций плательщика.
    /// </summary>
    [DataField]
    public LocId FineOutReason = "bank-transaction-terminal-fine-out";

    /// <summary>
    /// Описание штрафа в истории операций бюджета станции.
    /// </summary>
    [DataField]
    public LocId FineBudgetReason = "bank-transaction-terminal-fine-in";

    /// <summary>
    /// Предельная длина назначения счёта: более длинное назначение обрезается.
    /// </summary>
    [DataField]
    public int MaxPurposeLength = PaymentTerminalLimits.MaxPurposeLength;

    /// <summary>
    /// Предельная сумма счёта в кредитах.
    /// </summary>
    [DataField]
    public int MaxAmount = 100000;
}
