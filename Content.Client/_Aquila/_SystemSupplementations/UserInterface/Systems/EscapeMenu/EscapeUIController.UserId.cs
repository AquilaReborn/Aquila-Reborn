using Content.Client._Aquila.UserId;

namespace Content.Client.UserInterface.Systems.EscapeMenu;

public sealed partial class EscapeUIController
{
    private void InitializeAquilaButtons(Options.UI.EscapeMenu window)
    {
        window.UserIdButton.OnPressed += _ =>
        {
            CloseEscapeWindow();
            UIManager.GetUIController<UserIdUIController>().ToggleWindow();
        };
    }
}
