using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Cargo.Systems;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo.Components;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.VendingMachines;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class PaidVendingMachineSystem : SharedPaidVendingMachineSystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly BankPinSystem _bankPin = default!;
    [Dependency] private readonly CargoSystem _cargo = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedVendingMachineSystem _vending = default!;
    [Dependency] private readonly StationBudgetLogSystem _budgetLog = default!;
    [Dependency] private readonly StationSystem _station = default!;

    private static readonly TimeSpan PinSessionDuration = TimeSpan.FromSeconds(30);

    private static readonly LocId RevenueReason = "bank-budget-log-vending";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaidVendingMachineComponent, VendingMachinePurchaseAttemptEvent>(OnVendingMachinePurchaseAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var curTime = _timing.CurTime;
        var query = EntityQueryEnumerator<PaidVendingMachineComponent>();
        while (query.MoveNext(out _, out var vending))
        {
            if (vending.PinSessions.Count == 0)
                continue;

            List<EntityUid>? expired = null;
            foreach (var (buyer, end) in vending.PinSessions)
            {
                if (end > curTime && !TerminatingOrDeleted(buyer))
                    continue;

                expired ??= new List<EntityUid>();
                expired.Add(buyer);
            }

            if (expired is null)
                continue;

            foreach (var buyer in expired)
                vending.PinSessions.Remove(buyer);
        }
    }

    private void OnVendingMachinePurchaseAttempt(Entity<PaidVendingMachineComponent> ent, ref VendingMachinePurchaseAttemptEvent args)
    {
        if (!CanBuy(ent.AsNullable(), args.User, args.ItemId, args.Type))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-restricted"));
            args.Cancelled = true;
            return;
        }

        var price = GetPriceFor(ent.AsNullable(), args.User, args.ItemId);
        if (price <= 0)
            return;

        if (!_idCard.TryFindIdCard(args.User, out var idCard) || !_bank.TryGetAccount(idCard.Owner, out var account))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-no-account"));
            args.Cancelled = true;
            return;
        }

        if (!TryVerifyPurchasePin(ent, args.User, account.Value, args.ItemId, args.Type))
        {
            args.Cancelled = true;
            return;
        }

        if (!_bank.TryWithdraw(account.Value.AsNullable(), price, ent.Comp.PurchaseReason, Name(ent)))
        {
            Refuse(ent, args.User, Loc.GetString("vending-paid-insufficient", ("price", price)));
            args.Cancelled = true;
            return;
        }

        if (_station.GetOwningStation(ent) is { } station && TryComp<StationBankAccountComponent>(station, out var stationBank))
        {
            var revenueAccount = GetRevenueAccount(ent);
            var revenue = (int) (price * ent.Comp.RevenueShare);
            if (_cargo.TryAdjustBankAccount((station, stationBank), revenueAccount, revenue))
                _budgetLog.LogTransaction(station, revenueAccount, revenue, RevenueReason, Name(ent));
        }

        if (TryComp<VendingMachineComponent>(ent, out var vending))
            _audio.PlayEntity(vending.SoundVend, args.User, ent);

        _audio.PlayEntity(ent.Comp.PurchaseSound, args.User, ent);
        _popup.PopupEntity(Loc.GetString("vending-paid-purchased", ("price", price)), ent, args.User);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} paid {price} credits for {args.ItemId} at {ToPrettyString(ent):vendor}");
    }

    private ProtoId<CargoAccountPrototype> GetRevenueAccount(Entity<PaidVendingMachineComponent> ent)
    {
        if (!ent.Comp.RevenueByDepartment)
            return ent.Comp.RevenueAccount;

        if (!TryComp<AccessReaderComponent>(ent, out var reader))
            return ent.Comp.RevenueAccount;

        foreach (var accessList in reader.AccessLists)
        {
            foreach (var (access, account) in ent.Comp.DepartmentAccounts)
            {
                if (accessList.Contains(access))
                    return account;
            }
        }

        return ent.Comp.RevenueAccount;
    }

    private bool TryVerifyPurchasePin(Entity<PaidVendingMachineComponent> ent, EntityUid user, Entity<BankAccountComponent> account, string itemId, InventoryType type)
    {
        if (ent.Comp.PinSessions.TryGetValue(user, out var sessionEnd) && sessionEnd > _timing.CurTime)
            return true;

        return _bankPin.TryVerifyPin(account.AsNullable(), BankSecurityLevels.Purchase, null, user, ent.Owner,
            _ => RetryPurchaseAfterPin(ent, user, itemId, type),
            () => _vending.Deny(ent.Owner));
    }

    private void RetryPurchaseAfterPin(Entity<PaidVendingMachineComponent> ent, EntityUid user, string itemId, InventoryType type)
    {
        ent.Comp.PinSessions[user] = _timing.CurTime + PinSessionDuration;

        if (TryComp<VendingMachineComponent>(ent.Owner, out var vending))
            _vending.AuthorizedVend(ent.Owner, user, type, itemId, vending);
    }

    private void Refuse(EntityUid vendor, EntityUid user, string message)
    {
        _popup.PopupEntity(message, vendor, user);
        _vending.Deny(vendor);
    }
}
