using Content.Server._Aquila.Economy.Components;
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

    private const int FailedPinAlertThreshold = 3;
    private static readonly TimeSpan FailedPinWindow = TimeSpan.FromMinutes(5);

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
        return TryVerifyPin(account.Owner, account.Comp, level, pin, user, retry, wrongPin);
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
            });
    }

    private bool TryVerifyPin(
        EntityUid owner,
        BankAccountComponent account,
        int level,
        int? pin,
        EntityUid user,
        Action<int> retry,
        Action wrongPin)
    {
        if (account.SecurityLevel < level || account.Pin == BankAccountComponent.NoPin)
            return true;

        if (pin is { } entered)
        {
            if (_bank.CheckPin(new Entity<BankAccountComponent?>(owner, account), entered))
            {
                account.FailedPinAttempts = 0;
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
    /// Регистрирует неудачную попытку ввода PIN для счёта. Если попыток за короткое время стало слишком много,
    /// поднимает <see cref="BankPinFailedEvent"/>.
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
            return;

        account.FailedPinAttempts = 0;
        var ev = new BankPinFailedEvent(owner);
        RaiseLocalEvent(ref ev);
    }
}
