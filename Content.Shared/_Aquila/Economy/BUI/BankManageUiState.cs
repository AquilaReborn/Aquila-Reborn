using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankManageUiState(
    string budgetName,
    int budgetBalance,
    int bonusLeft,
    int? maxFine,
    int maxCommentLength,
    List<BankManageRecordUiData> requests,
    List<BankManageRecordUiData> fines)
{
    public readonly string BudgetName = budgetName;
    public readonly int BudgetBalance = budgetBalance;
    public readonly int BonusLeft = bonusLeft;
    public readonly int? MaxFine = maxFine;
    public readonly int MaxCommentLength = maxCommentLength;
    public readonly List<BankManageRecordUiData> Requests = requests;
    public readonly List<BankManageRecordUiData> Fines = fines;
}
