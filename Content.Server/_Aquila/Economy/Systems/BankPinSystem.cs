using Content.Server._Aquila.Economy.Components;
using Content.Server._Aquila.Economy.Events;
using Content.Server.Administration;
using Content.Shared.Popups;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Server._Aquila.Economy.Systems;

/// <summary>
/// Запрос и проверка PIN банковского счёта перед защищёнными операциями. Общий для всех мест, где операция по счёту защищена PIN.
/// </summary>
public sealed class BankPinSystem : EntitySystem
{
    [Dependency] private readonly BankAccountSystem _bank = default!;
    [Dependency] private readonly QuickDialogSystem _quickDialog = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly LocId PinDialogTitle = "bank-pin-dialog-title";
    private static readonly LocId PinDialogPrompt = "bank-pin-prompt";
    private static readonly LocId WrongPinMessage = "bank-pin-wrong";
    private static readonly LocId LockedPinMessage = "bank-pin-locked";

    private const int FailedPinAlertThreshold = 3;
    private static readonly TimeSpan FailedPinWindow = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FailedPinLockout = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan FailedPinSeriesLockout = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Проверяет, заблокирован ли ввод PIN счёта после неудачных попыток.
    /// </summary>
    /// <param name="account">Счёт, чей PIN проверяют. Может быть не разрешён.</param>
    /// <returns>true, если ввод PIN счёта ещё заблокирован.</returns>
    public bool IsPinLocked(Entity<BankAccountComponent?> account)
    {
        if (!Resolve(account, ref account.Comp, false))
            return false;

        return _timing.CurTime < account.Comp.PinLockoutUntil;
    }

    /// <summary>
    /// Проверяет PIN счёта перед защищённой операцией. Если счёт защищён PIN на указанном уровне, а PIN ещё не введён,
    /// открывает игроку диалог и повторяет операцию через переданный обработчик с введённым PIN.
    /// </summary>
    /// <param name="account">Счёт, чей PIN проверяют.</param>
    /// <param name="level">Уровень защиты счёта, начиная с которого нужен PIN.</param>
    /// <param name="pin">Введённый PIN или null, если игрока ещё не спрашивали.</param>
    /// <param name="user">Игрок, который проводит операцию.</param>
    /// <param name="retry">Обработчик, который повторяет операцию с введённым PIN.</param>
    /// <param name="wrongPin">Обработчик отказа, если PIN не подошёл.</param>
    /// <returns>true, если PIN верный или операция не защищена PIN. false, если операцию нужно прервать: диалог открыт или PIN неверный.</returns>
    public bool TryVerifyPin(
        Entity<BankAccountComponent> account,
        int level,
        int? pin,
        EntityUid user,
        Action<int> retry,
        Action wrongPin)
    {
        return TryVerifyPin(account.Owner, account.Comp, level, pin, user, retry, wrongPin, wrongPin);
    }

    /// <summary>
    /// Проверяет PIN счёта перед защищённой операцией. Сам проверяет, что счёт, игрок и источник попапа ещё существуют,
    /// и сам показывает попап об ошибке, если PIN не подошёл.
    /// </summary>
    /// <param name="account">Счёт, чей PIN проверяют. Может быть не разрешён.</param>
    /// <param name="level">Уровень защиты счёта, начиная с которого нужен PIN.</param>
    /// <param name="pin">Введённый PIN или null, если игрока ещё не спрашивали.</param>
    /// <param name="user">Игрок, который проводит операцию.</param>
    /// <param name="source">Сущность, у которой показывается попап об ошибке.</param>
    /// <param name="retry">Обработчик, который повторяет операцию с введённым PIN.</param>
    /// <param name="denyFeedback">Дополнительный отказ: звук или анимация. Попап показывается всегда.</param>
    /// <returns>true, если PIN верный или операция не защищена PIN. false, если операцию нужно прервать: диалог открыт или PIN неверный.</returns>
    public bool TryVerifyPin(
        Entity<BankAccountComponent?> account,
        int level,
        int? pin,
        EntityUid user,
        EntityUid source,
        Action<int> retry,
        Action? denyFeedback = null)
    {
        if (!Resolve(account, ref account.Comp, false)
            || TerminatingOrDeleted(account.Owner)
            || TerminatingOrDeleted(user)
            || TerminatingOrDeleted(source))
            return false;

        var owner = account.Owner;
        return TryVerifyPin(owner, account.Comp, level, pin, user,
            entered =>
            {
                if (TerminatingOrDeleted(owner) || TerminatingOrDeleted(user) || TerminatingOrDeleted(source))
                    return;

                retry(entered);
            },
            () =>
            {
                _popup.PopupEntity(Loc.GetString(WrongPinMessage), source, user);
                denyFeedback?.Invoke();
            },
            () =>
            {
                _popup.PopupEntity(Loc.GetString(LockedPinMessage), source, user);
                denyFeedback?.Invoke();
            });
    }

    private bool TryVerifyPin(
        EntityUid owner,
        BankAccountComponent account,
        int level,
        int? pin,
        EntityUid user,
        Action<int> retry,
        Action wrongPin,
        Action lockedPin)
    {
        if (account.SecurityLevel < level || account.Pin == BankAccountComponent.NoPin)
            return true;

        if (_timing.CurTime < account.PinLockoutUntil)
        {
            lockedPin();
            return false;
        }

        if (pin is { } entered)
        {
            if (_bank.CheckPin(new Entity<BankAccountComponent?>(owner, account), entered))
            {
                account.FailedPinAttempts = 0;
                account.PinLockoutUntil = default;
                return true;
            }

            RegisterFailedPin(owner, account);
            wrongPin();
            return false;
        }

        if (!TryComp<ActorComponent>(user, out var actor))
            return false;

        _quickDialog.OpenDialog<string>(actor.PlayerSession,
            Loc.GetString(PinDialogTitle),
            Loc.GetString(PinDialogPrompt),
            text =>
            {
                if (!int.TryParse(text.Trim(), out var parsed))
                {
                    wrongPin();
                    return;
                }

                retry(parsed);
            });

        return false;
    }

    /// <summary>
    /// Регистрирует неудачную попытку ввода PIN для счёта. Блокирует ввод PIN на короткое время, а если попыток
    /// за короткое время стало слишком много, блокирует его надолго и поднимает <see cref="BankPinFailedEvent"/>.
    /// </summary>
    /// <param name="account">Счёт, для которого ввели неверный PIN. Может быть не разрешён.</param>
    public void NotifyFailedPin(Entity<BankAccountComponent?> account)
    {
        if (!Resolve(account, ref account.Comp, false))
            return;

        RegisterFailedPin(account.Owner, account.Comp);
    }

    private void RegisterFailedPin(EntityUid owner, BankAccountComponent account)
    {
        if (TerminatingOrDeleted(owner))
            return;

        var now = _timing.CurTime;
        if (now - account.LastFailedPinTime > FailedPinWindow)
            account.FailedPinAttempts = 0;

        account.LastFailedPinTime = now;
        account.FailedPinAttempts++;

        if (account.FailedPinAttempts < FailedPinAlertThreshold)
        {
            account.PinLockoutUntil = now + FailedPinLockout;
            return;
        }

        account.FailedPinAttempts = 0;
        account.PinLockoutUntil = now + FailedPinSeriesLockout;
        var ev = new BankPinFailedEvent(owner);
        RaiseLocalEvent(ref ev);
    }
}
