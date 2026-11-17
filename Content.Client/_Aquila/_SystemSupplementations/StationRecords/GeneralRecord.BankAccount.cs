using Content.Shared.StationRecords;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.StationRecords;

public sealed partial class GeneralRecord
{
    private static readonly LocId BankAccountText = "general-station-record-console-record-bank-account";
    private static readonly LocId BankBalanceText = "general-station-record-console-record-bank-balance";
    private static readonly LocId BankSalaryText = "general-station-record-console-record-bank-salary";

    private void AquilaSetupBankAccount(GeneralStationRecord record)
    {
        var contents = Contents;

        if (record.BankAccountNumber == null
            && record.BankBalance == null
            && record.BankSalary == null)
            return;

        contents.RemoveChild(DeleteButton);

        if (record.BankAccountNumber is { } number)
        {
            var button = CreateBankRow(out var label);
            label.Text = Loc.GetString(BankAccountText, ("number", number));
            button.OnPressed += _ => _clipboard.SetText(number.ToString());
            contents.AddChild(button);
        }

        if (record.BankBalance is { } balance)
        {
            var button = CreateBankRow(out var label);
            label.Text = Loc.GetString(BankBalanceText, ("balance", balance));
            button.OnPressed += _ => _clipboard.SetText(balance.ToString());
            contents.AddChild(button);
        }

        if (record.BankSalary is { } salary)
        {
            var button = CreateBankRow(out var label);
            label.Text = Loc.GetString(BankSalaryText, ("salary", salary));
            button.OnPressed += _ => _clipboard.SetText(salary.ToString());
            contents.AddChild(button);
        }

        contents.AddChild(DeleteButton);
    }

    private static ContainerButton CreateBankRow(out RichTextLabel label)
    {
        label = new RichTextLabel();
        var button = new ContainerButton();
        button.AddChild(label);
        return button;
    }
}
