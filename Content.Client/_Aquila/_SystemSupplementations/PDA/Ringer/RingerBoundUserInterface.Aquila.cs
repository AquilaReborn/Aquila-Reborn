using Content.Shared._Aquila.PDA.Ringer;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA.Ringer;

public sealed partial class RingerBoundUserInterface
{
    private void OnSilentToggled(BaseButton.ButtonToggledEventArgs args)
    {
        SendPredictedMessage(new RingerSetSilentMessage(args.Pressed));
    }

    /// <inheritdoc/>
    protected override void Dispose(bool disposing)
    {
        if (disposing && _menu != null)
            _menu.SilentCheckBox.OnToggled -= OnSilentToggled;

        base.Dispose(disposing);
    }
}
