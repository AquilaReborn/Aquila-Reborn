using Content.Server._Aquila.Economy.Systems;
using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared._Aquila.Economy.Types;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Aquila.Economy.Components;

[RegisterComponent, AutoGenerateComponentPause, Access(typeof(BankAccountSystem), typeof(BankPinSystem))]
public sealed partial class BankAccountComponent : Component
{
    /// <summary>
    /// Номер счёта, по которому на него переводят деньги.
    /// </summary>
    [DataField]
    public int Number;

    /// <summary>
    /// Наименьший возможный номер счёта.
    /// </summary>
    [DataField]
    public int MinNumber = 100000;

    /// <summary>
    /// Наибольший возможный номер счёта, не включительно.
    /// </summary>
    [DataField]
    public int MaxNumber = 1000000;

    /// <summary>
    /// Имя владельца счёта.
    /// </summary>
    [DataField]
    public string OwnerName = string.Empty;

    /// <summary>
    /// Станция, к которой привязан счёт. Заполняется при открытии счёта.
    /// </summary>
    [ViewVariables]
    public EntityUid? Station;

    /// <summary>
    /// Должность владельца, по которой начисляется зарплата.
    /// </summary>
    [DataField]
    public ProtoId<JobPrototype>? Job;

    /// <summary>
    /// Текущий баланс.
    /// </summary>
    [DataField]
    public int Balance;

    /// <summary>
    /// Последние операции по счёту, от старых к новым.
    /// </summary>
    [DataField]
    public List<BankTransaction> History = new();

    /// <summary>
    /// Сколько операций хранится в истории.
    /// </summary>
    [DataField]
    public int MaxHistory = 100;

    /// <summary>
    /// Уровень защиты счёта: 0 - только карта, 1 - PIN при покупках в автоматах и терминале,
    /// 2 - карта и PIN в банкомате и КПК.
    /// </summary>
    [DataField]
    public int SecurityLevel = BankSecurityLevels.Open;

    /// <summary>
    /// Значение поля <see cref="Pin"/>, означающее, что PIN на счёте не задан.
    /// </summary>
    public const int NoPin = 0;

    /// <summary>
    /// Четырёхзначный PIN счёта от 1000 до 9999. Значение <see cref="NoPin"/> означает, что PIN не задан,
    /// тогда проверка PIN пропускает любую карту.
    /// </summary>
    [DataField]
    public int Pin;

    /// <summary>
    /// Сколько раз подряд неверно вводили PIN счёта.
    /// </summary>
    [ViewVariables]
    public int FailedPinAttempts;

    /// <summary>
    /// Когда последний раз неверно вводили PIN счёта.
    /// </summary>
    [ViewVariables]
    public TimeSpan LastFailedPinTime;

    /// <summary>
    /// До какого времени ввод PIN счёта заблокирован после неудачной попытки.
    /// </summary>
    [ViewVariables]
    public TimeSpan PinLockoutUntil;

    /// <summary>
    /// Владелец счёта, которому показывают PIN. Заполняется при открытии счёта, если владельца уже можно найти.
    /// </summary>
    [ViewVariables]
    public EntityUid? OwnerUid;

    /// <summary>
    /// Показывался ли владельцу PIN счёта.
    /// </summary>
    [ViewVariables]
    public bool PinNotified;

    /// <summary>
    /// Предел снятия наличных за одну операцию, 0 - без предела.
    /// </summary>
    [DataField]
    public int MaxCashWithdrawal = 500;

    /// <summary>
    /// Зарплаты по должностям.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, int> Salaries = new();

    /// <summary>
    /// Зарплата должностей, которых нет в списке.
    /// </summary>
    [DataField]
    public int DefaultSalary;

    /// <summary>
    /// Зарплата владельца счёта.
    /// </summary>
    [DataField]
    public int Salary;

    /// <summary>
    /// Сколько зарплат зачисляется сразу при открытии счёта.
    /// </summary>
    [DataField]
    public int StartingPaychecks = 1;

    /// <summary>
    /// Как часто ЦК выплачивает зарплату.
    /// </summary>
    [DataField]
    public TimeSpan PaydayInterval = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Когда будет следующая зарплата.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextPayday;

    /// <summary>
    /// Сколько премий владелец счёта уже выписал за смену.
    /// </summary>
    [DataField]
    public int BonusesIssued;

    /// <summary>
    /// Описание зачисления подъёмных при открытии счёта.
    /// </summary>
    [DataField]
    public LocId StartingReason = "bank-transaction-starting";

    /// <summary>
    /// Описание зачисления зарплаты.
    /// </summary>
    [DataField]
    public LocId SalaryReason = "bank-transaction-salary";

    /// <summary>
    /// Описание входящего перевода.
    /// </summary>
    [DataField]
    public LocId TransferInReason = "bank-transaction-transfer-in";

    /// <summary>
    /// Описание исходящего перевода.
    /// </summary>
    [DataField]
    public LocId TransferOutReason = "bank-transaction-transfer-out";

    /// <summary>
    /// Описание пополнения бюджета отдела с личного счёта.
    /// </summary>
    [DataField]
    public LocId BudgetDepositReason = "bank-transaction-budget-deposit";

    /// <summary>
    /// От чьего имени администрация проводит операции по счёту.
    /// </summary>
    [DataField]
    public LocId CentralCommandParty = "bank-party-central-command";

    /// <summary>
    /// Описание премии от администрации.
    /// </summary>
    [DataField]
    public LocId AdminBonusReason = "bank-transaction-bonus";

    /// <summary>
    /// Описание штрафа от администрации.
    /// </summary>
    [DataField]
    public LocId AdminFineReason = "bank-transaction-fine";

    /// <summary>
    /// Описание отмены операции администрацией.
    /// </summary>
    [DataField]
    public LocId RevertReason = "bank-transaction-reverted";

    /// <summary>
    /// Описание исправления баланса администрацией.
    /// </summary>
    [DataField]
    public LocId AdjustReason = "bank-transaction-adjusted";

    /// <summary>
    /// Сообщение с PIN, которое владелец видит при открытии счёта.
    /// </summary>
    [DataField]
    public LocId PinGeneratedMessage = "bank-pin-generated";
}
