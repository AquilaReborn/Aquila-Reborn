using Content.Shared.Atmos.Monitor.Components;
using Content.Shared.Interaction;

namespace Content.Server.Atmos.Monitor.Systems;

public partial class AirAlarmSystem
{
    private bool TryOpenAirAlarmUi(EntityUid uid, ActivateInWorldEvent args)
    {
        if (!args.Handled)
        {
            _ui.OpenUi(uid, SharedAirAlarmInterfaceKey.Key, args.User);
            args.Handled = true;
        }

        return _ui.IsUiOpen(uid, SharedAirAlarmInterfaceKey.Key);
    }
}
