using Content.Server._Aquila.Economy.Components;
using Content.Server._Aquila.Economy.Events;
using Content.Server.Radio.EntitySystems;
using Content.Server.Station.Systems;
using Content.Shared.Radio;
using Content.Shared.Station.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

/// <summary>
/// Сообщает по радио службе безопасности станции о крупных банковских операциях и о серии неудачных попыток ввода PIN.
/// </summary>
public sealed class BankSecurityAlertSystem : EntitySystem
{
    [Dependency] private readonly RadioSystem _radio = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<RadioChannelPrototype> AlertChannel = "Security";
    private static readonly LocId AlertText = "bank-security-alert-transaction";
    private static readonly LocId PinFailedAlertText = "bank-security-alert-pin-failed";
    private static readonly TimeSpan AlertCooldown = TimeSpan.FromSeconds(10);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BankTransactionEvent>(OnBankTransaction);
        SubscribeLocalEvent<BankPinFailedEvent>(OnBankPinFailed);
    }

    private void OnBankPinFailed(ref BankPinFailedEvent args)
    {
        if (TerminatingOrDeleted(args.Account)
            || !TryComp<BankAccountComponent>(args.Account, out var account))
            return;

        if (GetAlertSource(account.Station) is not { } source || !TryUseAlertTime(account.Station))
            return;

        _radio.SendRadioMessage(source, Loc.GetString(PinFailedAlertText), AlertChannel, source);
    }

    private void OnBankTransaction(ref BankTransactionEvent args)
    {
        if (TerminatingOrDeleted(args.Account)
            || !TryComp<BankAccountComponent>(args.Account, out var account))
            return;

        var transaction = args.Transaction;
        if (transaction.Amount >= 0)
            return;

        var amount = -transaction.Amount;
        if (amount < GetThreshold(account.Station))
            return;

        if (transaction.Reason == account.SalaryReason || transaction.Reason == account.StartingReason)
            return;

        if (TryComp<StationTreasuryComponent>(account.Station, out var treasury)
            && transaction.Reason == treasury.SubsidyReason)
            return;

        if (GetAlertSource(account.Station) is not { } source || !TryUseAlertTime(account.Station))
            return;

        var reason = Loc.GetString(transaction.Reason, ("party", transaction.Party ?? string.Empty));
        _radio.SendRadioMessage(source, Loc.GetString(AlertText, ("amount", amount), ("reason", reason)), AlertChannel, source);
    }

    private bool TryUseAlertTime(EntityUid? station)
    {
        if (station is not { } stationUid || !TryComp<StationTreasuryComponent>(stationUid, out var treasury))
            return true;

        var now = _timing.CurTime;
        if (now < treasury.NextAlertTime)
            return false;

        treasury.NextAlertTime = now + AlertCooldown;
        return true;
    }

    private int GetThreshold(EntityUid? station)
    {
        return TryComp<StationTreasuryComponent>(station, out var treasury)
            ? treasury.LargeTransactionThreshold
            : StationTreasuryComponent.DefaultLargeTransactionThreshold;
    }

    private EntityUid? GetAlertSource(EntityUid? station)
    {
        if (station is not { } stationUid
            || !TryComp<StationDataComponent>(stationUid, out var stationData))
            return null;

        return _station.GetLargestGrid((stationUid, stationData));
    }
}
