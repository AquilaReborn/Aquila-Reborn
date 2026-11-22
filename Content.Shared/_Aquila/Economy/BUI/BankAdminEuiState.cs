using Content.Shared._Aquila.Economy.DataDefinitions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Economy.BUI;

[Serializable, NetSerializable]
public sealed class BankAdminEuiState(
    List<BankAdminAccountData> accounts,
    int? selected,
    List<BankTransaction> history,
    List<BankManageRecordUiData> requests,
    List<BankManageRecordUiData> fines)
    : EuiStateBase
{
    public readonly List<BankAdminAccountData> Accounts = accounts;
    public readonly int? Selected = selected;
    public readonly List<BankTransaction> History = history;
    public readonly List<BankManageRecordUiData> Requests = requests;
    public readonly List<BankManageRecordUiData> Fines = fines;
}
