using Content.Shared.Cargo.Components;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.Cargo.BUI;

public sealed partial class CargoOrderConsoleBoundUserInterface
{
    private void OnPersonalOrderToggled(ButtonToggledEventArgs args)
    {
        if (_product is null
            || _orderMenu is null
            || _menu is null
            || !EntMan.TryGetComponent<CargoOrderConsoleComponent>(Owner, out var orderConsole))
            return;

        _orderMenu.ToggleDepartmentSecureCrate.Pressed = false;
        _orderMenu.ToggleDepartmentSecureCrate.Disabled = args.Pressed
            || !_protoManager.TryIndex<EntityPrototype>(_product.Product, out var product)
            || !_cargoSystem.CanBeSecuredDelivery((Owner, orderConsole), product);
        _orderMenu.PointCost.Text = Loc.GetString("cargo-console-menu-points-amount", ("amount", _menu.GetProductCost(_product)));
    }
}
