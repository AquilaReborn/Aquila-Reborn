using System.Diagnostics.CodeAnalysis;
using Content.Server._Aquila.Economy.Components;
using Content.Server.Administration.Logs;
using Content.Server.Stack;
using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access.Systems;
using Content.Shared.Database;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Robust.Shared.Audio.Systems;

namespace Content.Server._Aquila.Economy.Systems;

public sealed class AtmSystem : EntitySystem
{
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly BankPinSystem _bankPin = default!;
    [Dependency] private readonly EconomyZoneSystem _economyZone = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly StackSystem _stack = default!;

    private static readonly LocId AtmFreeZoneMessage = "atm-free-zone";
    private static readonly LocId AtmOutOfRangeMessage = "shared-interaction-system-in-range-unobstructed-cannot-reach";
    private static readonly LocId AtmWithdrawUnavailableMessage = "atm-withdraw-unavailable";
    private static readonly LocId BankPinInvalidMessage = "bank-pin-invalid";
    private static readonly LocId BankPinWrongMessage = "bank-pin-wrong";

    public override void Initialize()
    {
        base.Initialize();

        Subs.BuiEvents<AtmComponent>(AtmUiKey.Key, subs =>
        {
            subs.Event<BoundUIOpenedEvent>(OnBoundUIOpened);
            subs.Event<AtmWithdrawMessage>(OnAtmWithdraw);
            subs.Event<AtmOpenAccountMessage>(OnAtmOpenAccount);
            subs.Event<AtmChangePinMessage>(OnAtmChangePin);
            subs.Event<AtmSetSecurityLevelMessage>(OnAtmSetSecurityLevel);
        });

        SubscribeLocalEvent<AtmComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnBoundUIOpened(Entity<AtmComponent> ent, ref BoundUIOpenedEvent args)
    {
        UpdateUiState(ent, args.Actor);
    }

    private void OnAtmOpenAccount(Entity<AtmComponent> ent, ref AtmOpenAccountMessage args)
    {
        if (!_economyZone.IsEconomyEnabled())
        {
            Refuse(ent, args.Actor, Loc.GetString("bank-offline"));
            return;
        }

        if (!_idCard.TryFindIdCard(args.Actor, out var idCard) || !_bank.TryOpenAccount(idCard.Owner, Name(args.Actor), out var account))
        {
            Refuse(ent, args.Actor, Loc.GetString("bank-open-account-failed"));
            return;
        }

        var number = account.Value.Comp.Number;
        _popup.PopupEntity(Loc.GetString("bank-open-account-success", ("number", number.ToString())), ent, args.Actor);
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.Actor):user} opened bank account {number} for {ToPrettyString(idCard):card} at {ToPrettyString(ent):atm}");
        UpdateUiState(ent, args.Actor);
    }

    private void OnAtmWithdraw(Entity<AtmComponent> ent, ref AtmWithdrawMessage args)
    {
        Withdraw(ent, args.Actor, args.Amount);
    }

    private void Withdraw(Entity<AtmComponent> ent, EntityUid user, int amount, int? pin = null)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(user))
            return;

        if (!_interaction.InRangeUnobstructed(user, ent.Owner))
        {
            Refuse(ent, user, Loc.GetString(AtmOutOfRangeMessage));
            return;
        }

        if (!CanWithdraw(ent, user, amount, out var account))
            return;

        if (!_bankPin.TryVerifyPin(account.AsNullable(), BankSecurityLevels.Full, pin, user, ent.Owner,
                entered => Withdraw(ent, user, amount, entered),
                () => _audio.PlayPvs(ent.Comp.DenySound, ent)))
            return;

        if (!_bank.TryWithdraw(account.AsNullable(), amount, ent.Comp.WithdrawReason))
        {
            Refuse(ent, user, Loc.GetString("bank-transfer-insufficient"));
            UpdateUiState(ent, user);
            return;
        }

        foreach (var cash in _stack.SpawnMultipleAtPosition(ent.Comp.CashStack, amount, Transform(user).Coordinates))
        {
            _hands.PickupOrDrop(user, cash);
        }

        _audio.PlayPvs(ent.Comp.WithdrawSound, ent);

        var number = account.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(user):user} withdrew {amount} credits from account {number} at {ToPrettyString(ent):atm}");
        UpdateUiState(ent, user);
    }

    private void OnAtmChangePin(Entity<AtmComponent> ent, ref AtmChangePinMessage args)
    {
        if (!TryGetActiveAccount(ent, args.Actor, out var account))
            return;

        if (!_bank.TryChangePin(account.AsNullable(), args.OldPin, args.NewPin))
        {
            if (_bank.CheckPin(account.AsNullable(), args.OldPin))
            {
                Refuse(ent, args.Actor, Loc.GetString(BankPinInvalidMessage));
                return;
            }

            _bankPin.NotifyFailedPin(account.AsNullable());
            Refuse(ent, args.Actor, Loc.GetString(BankPinWrongMessage));
            return;
        }

        _popup.PopupEntity(Loc.GetString("bank-pin-changed"), ent, args.Actor);
        UpdateUiState(ent, args.Actor);
    }

    private void OnAtmSetSecurityLevel(Entity<AtmComponent> ent, ref AtmSetSecurityLevelMessage args)
    {
        SetSecurityLevel(ent, args.Actor, args.Level);
    }

    private void SetSecurityLevel(Entity<AtmComponent> ent, EntityUid user, int level, int? pin = null)
    {
        if (TerminatingOrDeleted(ent) || TerminatingOrDeleted(user))
            return;

        if (!TryGetActiveAccount(ent, user, out var account))
            return;

        if (level > BankSecurityLevels.Open && account.Comp.Pin == BankAccountComponent.NoPin)
        {
            Refuse(ent, user, Loc.GetString("bank-pin-none"));
            return;
        }

        if (pin == null && account.Comp.SecurityLevel == level)
        {
            UpdateUiState(ent, user);
            return;
        }

        if (!_bankPin.TryVerifyPin(account.AsNullable(), BankSecurityLevels.Open, pin, user, ent.Owner,
                entered => SetSecurityLevel(ent, user, level, entered),
                () => _audio.PlayPvs(ent.Comp.DenySound, ent)))
            return;

        if (!_bank.TrySetSecurityLevel(account.AsNullable(), level, pin ?? BankAccountComponent.NoPin))
        {
            Refuse(ent, user, Loc.GetString("atm-security-level-invalid"));
            return;
        }

        _popup.PopupEntity(Loc.GetString("atm-security-level-set", ("level", Loc.GetString(BankSecurityLevels.GetName(level)))), ent, user);
        UpdateUiState(ent, user);
    }

    private void OnInteractUsing(Entity<AtmComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled
            || !TryComp<StackComponent>(args.Used, out var stack)
            || stack.StackTypeId != ent.Comp.CashStack)
            return;

        args.Handled = true;

        if (!CheckActive(ent, args.User))
            return;

        if (!TryGetUserAccount(args.User, out var account))
        {
            Refuse(ent, args.User, Loc.GetString("atm-no-account"));
            return;
        }

        var amount = stack.Count;
        _bank.Deposit(account.Value.AsNullable(), amount, ent.Comp.DepositReason);
        QueueDel(args.Used);

        _audio.PlayPvs(ent.Comp.DepositSound, ent);
        _popup.PopupEntity(Loc.GetString("atm-deposited", ("amount", amount)), ent, args.User);

        var number = account.Value.Comp.Number;
        _adminLog.Add(LogType.Action, LogImpact.Low,
            $"{ToPrettyString(args.User):user} deposited {amount} credits to account {number} at {ToPrettyString(ent):atm}");

        if (_ui.IsUiOpen(ent.Owner, AtmUiKey.Key, args.User))
            UpdateUiState(ent, args.User);
    }

    private bool TryGetActiveAccount(Entity<AtmComponent> ent, EntityUid user, out Entity<BankAccountComponent> account)
    {
        account = default;
        if (!CheckActive(ent, user))
            return false;

        if (!TryGetUserAccount(user, out var found))
        {
            Refuse(ent, user, Loc.GetString("atm-no-account"));
            return false;
        }

        account = found.Value;
        return true;
    }

    private bool CanWithdraw(Entity<AtmComponent> ent, EntityUid user, int amount, out Entity<BankAccountComponent> account)
    {
        if (!TryGetActiveAccount(ent, user, out account))
            return false;

        if (!_bank.CanWithdrawCash(account.AsNullable(), amount, out var limit))
        {
            Refuse(ent, user, limit > 0
                ? Loc.GetString("atm-withdraw-limit", ("limit", limit))
                : Loc.GetString(AtmWithdrawUnavailableMessage));
            return false;
        }

        if (amount <= 0 || account.Comp.Balance < amount)
        {
            Refuse(ent, user, Loc.GetString("bank-transfer-insufficient"));
            UpdateUiState(ent, user);
            return false;
        }

        return true;
    }

    private bool CheckActive(Entity<AtmComponent> ent, EntityUid user)
    {
        if (_economyZone.TryCheckActive(user, ent, AtmFreeZoneMessage))
            return true;

        _audio.PlayPvs(ent.Comp.DenySound, ent);
        return false;
    }

    private void Refuse(Entity<AtmComponent> ent, EntityUid user, string message)
    {
        _popup.PopupEntity(message, ent, user);
        _audio.PlayPvs(ent.Comp.DenySound, ent);
    }

    private void UpdateUiState(Entity<AtmComponent> ent, EntityUid user)
    {
        if (!_economyZone.IsEconomyEnabled())
        {
            _ui.SetUiState(ent.Owner, AtmUiKey.Key, new AtmUiState(null, null, 0, offline: true));
            return;
        }

        var state = TryGetUserAccount(user, out var account)
            ? new AtmUiState(account.Value.Comp.Number, account.Value.Comp.OwnerName, account.Value.Comp.Balance,
                securityLevel: account.Value.Comp.SecurityLevel)
            : new AtmUiState(null, null, 0, canOpen: _idCard.TryFindIdCard(user, out var idCard) && _bank.CanOpenAccount(idCard.Owner));

        _ui.SetUiState(ent.Owner, AtmUiKey.Key, state);
    }

    private bool TryGetUserAccount(EntityUid user, [NotNullWhen(true)] out Entity<BankAccountComponent>? account)
    {
        account = null;
        return _idCard.TryFindIdCard(user, out var idCard) && _bank.TryGetAccount(idCard.Owner, out account);
    }
}
