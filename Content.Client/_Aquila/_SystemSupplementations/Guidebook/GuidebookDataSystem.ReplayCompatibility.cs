using Content.Shared.Guidebook;
using Robust.Shared.Network;

namespace Content.Client.Guidebook;

public sealed partial class GuidebookDataSystem
{
    [Dependency] private readonly INetManager _connection = default!;

    private void RequestGuidebookDataAquila()
    {
        if (_connection.IsConnected)
            RaiseNetworkEvent(new RequestGuidebookDataEvent());
    }
}
