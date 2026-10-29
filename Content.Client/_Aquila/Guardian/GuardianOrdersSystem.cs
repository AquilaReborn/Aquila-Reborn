using Content.Client.UserInterface.Controls;
using Content.Shared.Guardian;
using Content.Shared.Aquila.Guardian;
using Robust.Shared.Utility;

namespace Content.Client.Guardian;

public sealed class GuardianOrdersSystem : EntitySystem
{
    private SimpleRadialMenu? _menu;

    private static readonly ResPath OrdersRsiPath = new("/Textures/_Aquila/Mobs/Aliens/Guardians/guardianOrders.rsi");

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<GuardianToggleOrdersActionEvent>(OnToggleOrders);
    }

    private void OnToggleOrders(GuardianToggleOrdersActionEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (_menu != null)
        {
            CloseMenu();
            return;
        }

        var models = new RadialMenuOptionBase[]
        {
            new RadialMenuActionOption<GuardianOrderType>(HandleOrderPicked, GuardianOrderType.Follow)
            {
                ToolTip = Loc.GetString("guardian-order-follow"),
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(OrdersRsiPath, "guardianOrderFollow")),
            },
            new RadialMenuActionOption<GuardianOrderType>(HandleOrderPicked, GuardianOrderType.AttackTarget)
            {
                ToolTip = Loc.GetString("guardian-order-attack-target"),
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(OrdersRsiPath, "guardianOrderAttackTarget")),
            },
            new RadialMenuActionOption<GuardianOrderType>(HandleOrderPicked, GuardianOrderType.AttackAll)
            {
                ToolTip = Loc.GetString("guardian-order-attack-all"),
                IconSpecifier = RadialMenuIconSpecifier.With(new SpriteSpecifier.Rsi(OrdersRsiPath, "guardianOrderAttackAll")),
            },
        };

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(models);
        _menu.OnClose += CloseMenu;
        _menu.OpenOverMouseScreenPosition();
    }

    private void HandleOrderPicked(GuardianOrderType order)
    {
        RaisePredictiveEvent(new GuardianSetOrderEvent(order));
        CloseMenu();
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.OnClose -= CloseMenu;
        _menu.Dispose();
        _menu = null;
    }
}
