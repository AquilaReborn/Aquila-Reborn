using Content.Shared._Aquila.PDA.Ringer;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.PDA.Ringer;

public sealed partial class RingerBoundUserInterface
{
    private void OnSilentToggled(BaseButton.ButtonToggledEventArgs args)
    {
        SendPredictedMessage(new RingerSetSilentMessage(args.Pressed));
    }
}
