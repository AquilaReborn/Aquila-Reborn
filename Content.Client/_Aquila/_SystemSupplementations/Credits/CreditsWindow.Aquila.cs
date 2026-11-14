using Robust.Client.UserInterface.Controls;

namespace Content.Client.Credits;

public sealed partial class CreditsWindow
{
    private void HidePatronsTab()
    {
        TabContainer.SetTabVisible(PatronsTab, false);
    }
}
