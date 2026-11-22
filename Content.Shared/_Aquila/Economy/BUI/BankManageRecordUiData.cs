using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankManageRecordUiData(int id, bool fine, string issuerName, string targetName, string budgetName, int amount, string comment)
{
    public readonly int Id = id;
    public readonly bool Fine = fine;
    public readonly string IssuerName = issuerName;
    public readonly string TargetName = targetName;
    public readonly string BudgetName = budgetName;
    public readonly int Amount = amount;
    public readonly string Comment = comment;
}
