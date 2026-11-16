using Content.Server._Aquila.Economy.Systems;
using Content.Shared.StationRecords;

namespace Content.Server.StationRecords.Systems;

public sealed partial class GeneralStationRecordConsoleSystem
{
    [Dependency] private readonly BankAccountSystem _aquilaBank = default!;

    private void AquilaPopulateBankAccount(StationRecordKey key, GeneralStationRecord? record)
    {
        if (record == null)
            return;

        record.BankAccountNumber = null;
        record.BankBalance = null;
        record.BankSalary = null;

        var query = EntityQueryEnumerator<StationRecordKeyStorageComponent>();
        while (query.MoveNext(out var card, out var keyStorage))
        {
            if (keyStorage.Key is not { } stored || !stored.Equals(key))
                continue;

            if (!_aquilaBank.TryGetAccountInfo(card, out var number, out var balance, out var salary))
                continue;

            record.BankAccountNumber = number;
            record.BankBalance = balance;
            record.BankSalary = salary;
            return;
        }
    }
}
