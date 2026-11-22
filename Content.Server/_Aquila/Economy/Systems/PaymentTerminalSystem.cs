using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

/// <summary>
/// Платёжный терминал: привязка счёта получателя, выставление счёта и безналичная оплата картой покупателя.
/// </summary>
public sealed class PaymentTerminalSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly BankPinSystem _bankPin = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPaymentTerminalSystem _terminal = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationBudgetLogSystem _budgetLog = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    private static readonly TimeSpan PaymentCooldown = TimeSpan.FromSeconds(2);
    private static readonly LocId TerminalFreeZoneMessage = "payment-terminal-free-zone";
    private static readonly LocId NoReceiptMessage = "payment-terminal-no-receipt";
    private static readonly LocId FineFailedMessage = "payment-terminal-fine-failed";
    private static readonly LocId FineRefundReason = "bank-transaction-terminal-fine-refund";
    private static readonly LocId ReceiptText = "payment-terminal-receipt-text";
    private static readonly LocId ReceiptName = "ent-PaperReceipt";
    private static readonly EntProtoId ReceiptPrototype = "PaperReceipt";

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<PaymentTerminalComponent>(PaymentTerminalUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<TerminalSetInvoiceMessage>(OnSetInvoice);
            subs.Event<TerminalClearInvoiceMessage>(OnClearInvoice);
            subs.Event<TerminalPrintReceiptMessage>(OnPrintReceipt);
            subs.Event<TerminalLinkCardMessage>(OnLinkCard);
        });

        SubscribeLocalEvent<PaymentTerminalComponent, InteractUsingEvent>(OnInteractUsing);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<PaymentTerminalComponent>();
        while (query.MoveNext(out var uid, out var terminal))
        {
            _terminal.PruneLastPayments((uid, terminal), PaymentCooldown);
        }
    }

    private void OnBoundUIOpened(Entity<PaymentTerminalComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUi(ent);
    }

    private void OnLinkCard(Entity<PaymentTerminalComponent> ent, ref TerminalLinkCardMessage args)
    {
        LinkCard(ent, args.Actor);
    }

    private void OnInteractUsing(Entity<PaymentTerminalComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_idCard.TryGetIdCard(args.Used, out var idCard))
            return;

        args.Handled = true;
        Pay(ent, idCard, args.User);
    }

    private void OnSetInvoice(Entity<PaymentTerminalComponent> ent, ref TerminalSetInvoiceMessage args)
    {
        if (!CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
            return;

        if (args.Amount <= 0 || args.Amount > ent.Comp.MaxAmount)
        {
            Refuse(ent.Owner, args.Actor, Loc.GetString("payment-terminal-invalid-amount", ("max", ent.Comp.MaxAmount)));
            return;
        }

        var purpose = args.Purpose?.Trim() ?? string.Empty;
        if (purpose.Length == 0)
        {
            Refuse(ent.Owner, args.Actor, Loc.GetString("payment-terminal-no-purpose"));
            return;
        }

        if (purpose.Length > ent.Comp.MaxPurposeLength)
            purpose = purpose[..ent.Comp.MaxPurposeLength];

        _terminal.SetInvoice(ent.AsNullable(), args.Amount, purpose);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("payment-terminal-invoice-set", ("amount", args.Amount), ("purpose", purpose)), ent, args.Actor);
    }

    private void OnClearInvoice(Entity<PaymentTerminalComponent> ent, ref TerminalClearInvoiceMessage args)
    {
        if (!CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
            return;

        _terminal.ClearInvoice(ent.AsNullable());
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("payment-terminal-invoice-cleared"), ent, args.Actor);
    }

    private void OnPrintReceipt(Entity<PaymentTerminalComponent> ent, ref TerminalPrintReceiptMessage args)
    {
        if (TerminatingOrDeleted(ent.Owner))
            return;

        if (!CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
            return;

        if (ent.Comp.LastReceipt is not { } receipt)
        {
            Refuse(ent.Owner, args.Actor, Loc.GetString(NoReceiptMessage));
            return;
        }

        var paper = SpawnAtPosition(ReceiptPrototype, Transform(ent.Owner).Coordinates);
        if (!TryComp<PaperComponent>(paper, out var paperComp))
        {
            Log.Error($"Printed receipt {ToPrettyString(paper)} has no PaperComponent.");
            return;
        }

        var time = receipt.Time;
        var timeText = $"{(int) time.TotalHours:00}:{time.Minutes:00}";

        _metaData.SetEntityName(paper, Loc.GetString(ReceiptName));
        _paper.SetContent((paper, paperComp), Loc.GetString(ReceiptText,
            ("amount", receipt.Amount),
            ("purpose", receipt.Purpose),
            ("payer", receipt.PayerName),
            ("time", timeText)));
    }

    private void LinkCard(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        if (ent.Comp.FineOnly)
            return;

        if (!CheckActive(ent.Owner, user))
            return;

        if (!_accessReader.IsAllowed(user, ent.Owner))
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-access-denied"));
            return;
        }

        if (!_idCard.TryFindIdCard(user, out var idCard) || !_bank.TryGetAccount(idCard.Owner, out var account))
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-no-card"));
            return;
        }

        if (ent.Comp.LinkedAccount is { } linked && linked != account.Value.Owner)
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-already-linked"));
            return;
        }

        _terminal.SetLinkedAccount(ent.AsNullable(), account.Value.Owner);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString("payment-terminal-card-linked", ("name", account.Value.Comp.OwnerName)), ent, user);
    }

    private void Pay(Entity<PaymentTerminalComponent> ent, Entity<IdCardComponent> card, EntityUid user, int? pin = null)
    {
        if (!CheckActive(ent.Owner, user))
            return;

        Entity<BankAccountComponent>? receiver = null;
        if (!ent.Comp.FineOnly)
        {
            if (ent.Comp.LinkedAccount is not { } linked || !TryComp<BankAccountComponent>(linked, out var linkedAccount))
            {
                Refuse(ent.Owner, user, Loc.GetString("payment-terminal-no-linked-account"));
                return;
            }

            receiver = (linked, linkedAccount);
        }

        if (!ent.Comp.Ready || ent.Comp.Amount <= 0)
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-no-invoice"));
            return;
        }

        if (!_bank.TryGetAccount(card.Owner, out var payer))
        {
            Refuse(ent.Owner, user, Loc.GetString("bank-ui-no-account"));
            return;
        }

        if (receiver is { } target && payer.Value.Owner == target.Owner)
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-same-account"));
            return;
        }

        if (_terminal.IsPaymentOnCooldown(ent.AsNullable(), user, PaymentCooldown))
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-cooldown"));
            return;
        }

        if (!_bankPin.TryVerifyPin(payer.Value.AsNullable(), BankSecurityLevels.Purchase, pin, user, ent.Owner,
            value => PayAfterPin(ent, card, user, value)))
            return;

        var amount = ent.Comp.Amount;
        var purpose = ent.Comp.Purpose;
        string popup;
        if (ent.Comp.FineOnly)
        {
            if (!TryPayFine(ent, payer.Value, user, amount))
                return;

            _terminal.ClearInvoice(ent.AsNullable());
            popup = Loc.GetString("payment-terminal-fined", ("amount", amount));
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} paid a fine of {amount} credits from account {payer.Value.Comp.Number} into the {ent.Comp.FineBudget} budget at {ToPrettyString(ent):terminal}, purpose: {purpose}");
        }
        else
        {
            if (receiver is not { } destination)
                return;

            if (payer.Value.Comp.Balance < amount
                || !_bank.TryTransfer(payer.Value.AsNullable(), destination.AsNullable(), amount,
                    ent.Comp.PaymentOutReason, ent.Comp.PaymentInReason, purpose))
            {
                Refuse(ent.Owner, user, Loc.GetString("bank-transfer-insufficient"));
                return;
            }

            popup = Loc.GetString("payment-terminal-paid", ("amount", amount));
            _adminLog.Add(LogType.Action, LogImpact.Low,
                $"{ToPrettyString(user):user} paid {amount} credits from account {payer.Value.Comp.Number} to account {destination.Comp.Number} at {ToPrettyString(ent):terminal}, purpose: {purpose}");
        }

        var receipt = new PaymentTerminalReceipt(amount, payer.Value.Comp.OwnerName, purpose, _timing.CurTime);
        _terminal.SetReceipt(ent.AsNullable(), receipt);
        _terminal.SetLastPayment(ent.AsNullable(), user);
        UpdateUi(ent);
        _popup.PopupEntity(popup, ent, user);
    }

    private bool TryPayFine(Entity<PaymentTerminalComponent> ent, Entity<BankAccountComponent> payer, EntityUid user, int amount)
    {
        if (_station.GetOwningStation(ent) is not { } station
            || !TryComp<StationBankAccountComponent>(station, out var stationBank)
            || !_cargo.TryGetAccount((station, stationBank), ent.Comp.FineBudget, out _))
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-no-budget"));
            return false;
        }

        var budgetName = Loc.GetString(_proto.Index(ent.Comp.FineBudget).Name);
        if (!_bank.TryWithdraw(payer.AsNullable(), amount, ent.Comp.FineOutReason, budgetName, ent.Comp.Purpose))
        {
            Refuse(ent.Owner, user, Loc.GetString("bank-transfer-insufficient"));
            return false;
        }

        if (!_cargo.TryAdjustBankAccount((station, stationBank), ent.Comp.FineBudget, amount))
        {
            _bank.Deposit(payer.AsNullable(), amount, FineRefundReason, budgetName, ent.Comp.Purpose);
            Refuse(ent.Owner, user, Loc.GetString(FineFailedMessage));
            return false;
        }

        _budgetLog.LogTransaction(station, ent.Comp.FineBudget, amount, ent.Comp.FineBudgetReason, payer.Comp.OwnerName, ent.Comp.Purpose);

        return true;
    }

    private void PayAfterPin(Entity<PaymentTerminalComponent> ent, Entity<IdCardComponent> card, EntityUid user, int pin)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(card.Owner) || TerminatingOrDeleted(user))
            return;

        Pay(ent, card, user, pin);
    }

    private bool CheckActive(EntityUid ent, EntityUid user)
    {
        return _economyZone.TryCheckActive(user, ent, TerminalFreeZoneMessage);
    }

    private bool CheckOwner(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        if (!ent.Comp.FineOnly && ent.Comp.LinkedAccount is not { })
        {
            Refuse(ent.Owner, user, Loc.GetString("payment-terminal-no-linked-account"));
            return false;
        }

        if (CanManage(ent, user))
            return true;

        Refuse(ent.Owner, user, Loc.GetString(ent.Comp.FineOnly ? "payment-terminal-access-denied" : "payment-terminal-not-owner"));
        return false;
    }

    private bool CanManage(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        return ent.Comp.FineOnly
            ? _accessReader.IsAllowed(user, ent.Owner)
            : OwnsAccount(ent, user);
    }

    private bool OwnsAccount(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        return ent.Comp.LinkedAccount is { } linked
               && _idCard.TryFindIdCard(user, out var idCard)
               && _bank.TryGetAccount(idCard.Owner, out var account)
               && account.Value.Owner == linked;
    }

    private EntityUid? GetViewer(Entity<PaymentTerminalComponent> ent)
    {
        foreach (var actor in _ui.GetActors(ent.Owner, PaymentTerminalUiKey.Key))
            return actor;

        return null;
    }

    private void Refuse(EntityUid ent, EntityUid user, string message)
    {
        _popup.PopupEntity(message, ent, user);
    }

    private void UpdateUi(Entity<PaymentTerminalComponent> ent)
    {
        string? ownerName = null;
        if (ent.Comp.LinkedAccount is { } linked && TryComp<BankAccountComponent>(linked, out var account))
            ownerName = account.OwnerName;

        var budgetName = ent.Comp.FineOnly ? Loc.GetString(_proto.Index(ent.Comp.FineBudget).Name) : null;
        var isOwner = GetViewer(ent) is { } viewer && CanManage(ent, viewer);
        var canLinkCard = !ent.Comp.FineOnly && ent.Comp.LinkedAccount is null;

        var state = new PaymentTerminalUiState(ownerName, budgetName, ent.Comp.Amount, ent.Comp.Purpose, ent.Comp.Ready, isOwner, ent.Comp.FineOnly, canLinkCard);
        _ui.SetUiState(ent.Owner, PaymentTerminalUiKey.Key, state);
    }
}
