namespace Content.Shared.StationRecords;

public sealed partial record GeneralStationRecord
{
    /// <summary>
    /// Номер банковского счёта владельца записи.
    /// </summary>
    [DataField]
    public int? BankAccountNumber;

    /// <summary>
    /// Баланс банковского счёта владельца записи.
    /// </summary>
    [DataField]
    public int? BankBalance;

    /// <summary>
    /// Зарплата владельца записи.
    /// </summary>
    [DataField]
    public int? BankSalary;
}
