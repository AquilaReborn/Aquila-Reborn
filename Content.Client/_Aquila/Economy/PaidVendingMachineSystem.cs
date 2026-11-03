using Content.Shared._Aquila.Economy;
using Content.Shared._Aquila.Economy.Components;
using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.VendingMachines;
using Robust.Shared.Prototypes;

namespace Content.Client._Aquila.Economy;

public sealed class PaidVendingMachineSystem : SharedPaidVendingMachineSystem
{
    private static readonly LocId RestrictedLabel = "vending-paid-restricted-label";
    private static readonly LocId PriceLabel = "vending-paid-price";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaidVendingMachineComponent, VendingMachinePurchaseAttemptEvent>(OnVendingMachinePurchaseAttempt);
    }

    /// <summary>
    /// Подпись к товару в меню автомата: цена для покупателя или пометка, что товар ему не продаётся. Null, если товар бесплатный.
    /// </summary>
    public string? GetPriceLabel(Entity<PaidVendingMachineComponent?> vending, EntityUid user, EntProtoId item, InventoryType type)
    {
        if (!CanBuy(vending, user, item, type))
            return Loc.GetString(RestrictedLabel);

        var price = GetPriceFor(vending, user, item);
        return price > 0 ? Loc.GetString(PriceLabel, ("price", price)) : null;
    }

    private void OnVendingMachinePurchaseAttempt(Entity<PaidVendingMachineComponent> ent, ref VendingMachinePurchaseAttemptEvent args)
    {
        if (!CanBuy(ent.AsNullable(), args.User, args.ItemId, args.Type)
            || GetPriceFor(ent.AsNullable(), args.User, args.ItemId) > 0)
            args.Cancelled = true;
    }
}
