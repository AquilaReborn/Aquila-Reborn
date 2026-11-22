using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Types;
using Robust.Shared.Timing;

namespace Content.Shared._Aquila.Economy.Systems;

/// <summary>
/// Общая часть платёжного терминала: меняет поля компонента. Состояние интерфейса отправляет серверная система.
/// </summary>
public sealed class SharedPaymentTerminalSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    /// <summary>
    /// Привязывает счёт к терминалу или снимает привязку, если передан null. Новый счёт сбрасывает выставленный счёт.
    /// </summary>
    public void SetLinkedAccount(Entity<PaymentTerminalComponent?> ent, EntityUid? account)
    {
        if (!Resolve(ent, ref ent.Comp, false) || ent.Comp.LinkedAccount == account)
            return;

        ent.Comp.LinkedAccount = account;
        ClearReceipt(ent);
        ClearInvoice(ent);
    }

    /// <summary>
    /// Выставляет счёт терминала: сумму и назначение. После этого терминал готов принять оплату.
    /// </summary>
    public void SetInvoice(Entity<PaymentTerminalComponent?> ent, int amount, string purpose)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Amount = amount;
        ent.Comp.Purpose = purpose;
        ent.Comp.Ready = true;

        ClearReceipt(ent);
    }

    /// <summary>
    /// Снимает выставленный счёт: сумму, назначение и готовность принять оплату.
    /// </summary>
    public void ClearInvoice(Entity<PaymentTerminalComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.Amount = 0;
        ent.Comp.Purpose = string.Empty;
        ent.Comp.Ready = false;
    }

    /// <summary>
    /// Проверяет, не истёк ли кулдаун оплаты у плательщика.
    /// </summary>
    /// <param name="ent">Терминал, через который платят.</param>
    /// <param name="payer">Плательщик, для которого проверяют кулдаун.</param>
    /// <param name="cooldown">Длительность кулдауна.</param>
    /// <returns>True, если платить ещё нельзя.</returns>
    public bool IsPaymentOnCooldown(Entity<PaymentTerminalComponent?> ent, EntityUid payer, TimeSpan cooldown)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        return ent.Comp.LastPayments.TryGetValue(payer, out var last) && _timing.CurTime - last < cooldown;
    }

    /// <summary>
    /// Запоминает время оплаты плательщика для кулдауна.
    /// </summary>
    /// <param name="ent">Терминал, через который заплатили.</param>
    /// <param name="payer">Плательщик, чьё время запоминают.</param>
    public void SetLastPayment(Entity<PaymentTerminalComponent?> ent, EntityUid payer)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.LastPayments[payer] = _timing.CurTime;
    }

    /// <summary>
    /// Запоминает последнюю успешную оплату, из которой терминал печатает чек.
    /// </summary>
    /// <param name="ent">Терминал, через который заплатили.</param>
    /// <param name="receipt">Данные оплаты для чека.</param>
    public void SetReceipt(Entity<PaymentTerminalComponent?> ent, PaymentTerminalReceipt receipt)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.LastReceipt = receipt;
    }

    /// <summary>
    /// Забывает последнюю оплату: печатать чек не из чего.
    /// </summary>
    /// <param name="ent">Терминал, чью последнюю оплату забывают.</param>
    public void ClearReceipt(Entity<PaymentTerminalComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        ent.Comp.LastReceipt = null;
    }

    /// <summary>
    /// Убирает из кулдауна истёкшие записи и записи удалённых плательщиков.
    /// </summary>
    /// <param name="ent">Терминал, чьи записи чистят.</param>
    /// <param name="cooldown">Длительность кулдауна.</param>
    public void PruneLastPayments(Entity<PaymentTerminalComponent> ent, TimeSpan cooldown)
    {
        if (ent.Comp.LastPayments.Count == 0)
            return;

        var curTime = _timing.CurTime;
        List<EntityUid>? expired = null;
        foreach (var (payer, last) in ent.Comp.LastPayments)
        {
            if (last + cooldown > curTime && !TerminatingOrDeleted(payer))
                continue;

            expired ??= new List<EntityUid>();
            expired.Add(payer);
        }

        if (expired is null)
            return;

        foreach (var payer in expired)
            ent.Comp.LastPayments.Remove(payer);
    }
}
