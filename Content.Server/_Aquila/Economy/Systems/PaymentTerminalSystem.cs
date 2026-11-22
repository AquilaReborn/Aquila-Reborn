using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Station.Systems;
using Content.Shared._Aquila.Economy.BUI;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared._Aquila.Economy.Types;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Cargo;
using Content.Shared.Cargo.Components;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
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
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly BankPinSystem _bankPin = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly SharedCargoSystem _cargo = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPaymentTerminalSystem _terminal = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StationBudgetLogSystem _budgetLog = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly PaperSystem _paper = default!;

    private static readonly TimeSpan PaymentCooldown = TimeSpan.FromSeconds(2);
    private static readonly LocId TerminalFreeZoneMessage = "payment-terminal-free-zone";
    private static readonly LocId OutOfRangeMessage = "shared-interaction-system-in-range-unobstructed-cannot-reach";
    private static readonly LocId NoReceiptMessage = "payment-terminal-no-receipt";
    private static readonly LocId FineFailedMessage = "payment-terminal-fine-failed";
    private static readonly LocId FineRefundReason = "bank-transaction-terminal-fine-refund";
    private static readonly LocId ReceiptText = "payment-terminal-receipt-text";
    private static readonly LocId InvalidAmountMessage = "payment-terminal-invalid-amount";
    private static readonly LocId NoPurposeMessage = "payment-terminal-no-purpose";
    private static readonly LocId InvoiceSetMessage = "payment-terminal-invoice-set";
    private static readonly LocId InvoiceClearedMessage = "payment-terminal-invoice-cleared";
    private static readonly LocId AccessDeniedMessage = "payment-terminal-access-denied";
    private static readonly LocId NotOwnerMessage = "payment-terminal-not-owner";
    private static readonly LocId NoCardMessage = "payment-terminal-no-card";
    private static readonly LocId CardLinkedMessage = "payment-terminal-card-linked";
    private static readonly LocId AlreadyLinkedMessage = "payment-terminal-already-linked";
    private static readonly LocId NoLinkedAccountMessage = "payment-terminal-no-linked-account";
    private static readonly LocId NoInvoiceMessage = "payment-terminal-no-invoice";
    private static readonly LocId NoAccountMessage = "bank-ui-no-account";
    private static readonly LocId SameAccountMessage = "payment-terminal-same-account";
    private static readonly LocId CooldownMessage = "payment-terminal-cooldown";
    private static readonly LocId FinedMessage = "payment-terminal-fined";
    private static readonly LocId PaidMessage = "payment-terminal-paid";
    private static readonly LocId InsufficientFundsMessage = "bank-transfer-insufficient";
    private static readonly LocId NoBudgetMessage = "payment-terminal-no-budget";
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
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(args.Actor))
            return;

        if (!CheckInRange(ent.Owner, args.Actor) || !CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
            return;

        if (args.Amount <= 0 || args.Amount > ent.Comp.MaxAmount)
        {
            Refuse(ent.Owner, args.Actor, Loc.GetString(InvalidAmountMessage, ("max", ent.Comp.MaxAmount)));
            return;
        }

        var purpose = args.Purpose?.Trim() ?? string.Empty;
        if (purpose.Length == 0)
        {
            Refuse(ent.Owner, args.Actor, Loc.GetString(NoPurposeMessage));
            return;
        }

        if (purpose.Length > ent.Comp.MaxPurposeLength)
            purpose = purpose[..ent.Comp.MaxPurposeLength];

        _terminal.SetInvoice(ent.AsNullable(), args.Amount, purpose);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString(InvoiceSetMessage, ("amount", args.Amount), ("purpose", purpose)), ent, args.Actor);
    }

    private void OnClearInvoice(Entity<PaymentTerminalComponent> ent, ref TerminalClearInvoiceMessage args)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(args.Actor))
            return;

        if (!CheckInRange(ent.Owner, args.Actor) || !CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
            return;

        _terminal.ClearInvoice(ent.AsNullable());
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString(InvoiceClearedMessage), ent, args.Actor);
    }

    private void OnPrintReceipt(Entity<PaymentTerminalComponent> ent, ref TerminalPrintReceiptMessage args)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(args.Actor))
            return;

        if (!CheckInRange(ent.Owner, args.Actor) || !CheckActive(ent.Owner, args.Actor) || !CheckOwner(ent, args.Actor))
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

        _paper.SetContent((paper, paperComp), Loc.GetString(ReceiptText,
            ("amount", receipt.Amount),
            ("purpose", receipt.Purpose),
            ("payer", receipt.PayerName),
            ("time", timeText)));

        _audio.PlayPvs(ent.Comp.PrintSound, ent.Owner);
    }

    private void LinkCard(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(user))
            return;

        if (ent.Comp.FineOnly)
            return;

        if (!CheckInRange(ent.Owner, user) || !CheckActive(ent.Owner, user))
            return;

        if (!_accessReader.IsAllowed(user, ent.Owner))
        {
            Refuse(ent.Owner, user, Loc.GetString(AccessDeniedMessage));
            return;
        }

        if (!_idCard.TryFindIdCard(user, out var idCard) || !_bank.TryGetAccount(idCard.Owner, out var account))
        {
            Refuse(ent.Owner, user, Loc.GetString(NoCardMessage));
            return;
        }

        if (ent.Comp.LinkedAccount is { } linked && linked != account.Value.Owner)
        {
            Refuse(ent.Owner, user, Loc.GetString(AlreadyLinkedMessage));
            return;
        }

        _terminal.SetLinkedAccount(ent.AsNullable(), account.Value.Owner);
        UpdateUi(ent);
        _popup.PopupEntity(Loc.GetString(CardLinkedMessage, ("name", account.Value.Comp.OwnerName)), ent, user);
    }

    private void Pay(Entity<PaymentTerminalComponent> ent, Entity<IdCardComponent> card, EntityUid user, int? pin = null)
    {
        if (TerminatingOrDeleted(ent.Owner) || TerminatingOrDeleted(card.Owner) || TerminatingOrDeleted(user))
            return;

        if (!CheckInRange(ent.Owner, user) || !CheckActive(ent.Owner, user))
            return;

        Entity<BankAccountComponent>? receiver = null;
        if (!ent.Comp.FineOnly)
        {
            if (ent.Comp.LinkedAccount is not { } linked || !TryComp<BankAccountComponent>(linked, out var linkedAccount))
            {
                Refuse(ent.Owner, user, Loc.GetString(NoLinkedAccountMessage));
                return;
            }

            receiver = (linked, linkedAccount);
        }

        if (!ent.Comp.Ready || ent.Comp.Amount <= 0)
        {
            Refuse(ent.Owner, user, Loc.GetString(NoInvoiceMessage));
            return;
        }

        if (!_bank.TryGetAccount(card.Owner, out var payer))
        {
            Refuse(ent.Owner, user, Loc.GetString(NoAccountMessage));
            return;
        }

        if (receiver is { } target && payer.Value.Owner == target.Owner)
        {
            Refuse(ent.Owner, user, Loc.GetString(SameAccountMessage));
            return;
        }

        if (_terminal.IsPaymentOnCooldown(ent.AsNullable(), user, PaymentCooldown))
        {
            Refuse(ent.Owner, user, Loc.GetString(CooldownMessage));
            return;
        }

        if (!_bankPin.TryVerifyPin(payer.Value.AsNullable(), BankSecurityLevels.Purchase, pin, user, ent.Owner,
            value => Pay(ent, card, user, value)))
            return;

        var amount = ent.Comp.Amount;
        var purpose = ent.Comp.Purpose;
        string popup;
        if (ent.Comp.FineOnly)
        {
            if (!TryPayFine(ent, payer.Value, user, amount))
                return;

            _terminal.ClearInvoice(ent.AsNullable());
            popup = Loc.GetString(FinedMessage, ("amount", amount));
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
                Refuse(ent.Owner, user, Loc.GetString(InsufficientFundsMessage));
                return;
            }

            popup = Loc.GetString(PaidMessage, ("amount", amount));
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
            Refuse(ent.Owner, user, Loc.GetString(NoBudgetMessage));
            return false;
        }

        var budgetName = Loc.GetString(_proto.Index(ent.Comp.FineBudget).Name);
        if (!_bank.TryWithdraw(payer.AsNullable(), amount, ent.Comp.FineOutReason, budgetName, ent.Comp.Purpose))
        {
            Refuse(ent.Owner, user, Loc.GetString(InsufficientFundsMessage));
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

    private bool CheckActive(EntityUid ent, EntityUid user)
    {
        return _economyZone.TryCheckActive(user, ent, TerminalFreeZoneMessage);
    }

    private bool CheckInRange(EntityUid ent, EntityUid user)
    {
        if (_interaction.InRangeUnobstructed(user, ent))
            return true;

        Refuse(ent, user, Loc.GetString(OutOfRangeMessage));
        return false;
    }

    private bool CheckOwner(Entity<PaymentTerminalComponent> ent, EntityUid user)
    {
        if (!ent.Comp.FineOnly && ent.Comp.LinkedAccount is not { })
        {
            Refuse(ent.Owner, user, Loc.GetString(NoLinkedAccountMessage));
            return false;
        }

        if (CanManage(ent, user))
            return true;

        Refuse(ent.Owner, user, Loc.GetString(ent.Comp.FineOnly ? AccessDeniedMessage : NotOwnerMessage));
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
