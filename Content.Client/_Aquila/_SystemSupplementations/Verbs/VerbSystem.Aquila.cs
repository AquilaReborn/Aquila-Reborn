using Robust.Shared.Network;

namespace Content.Client.Verbs;

public sealed partial class VerbSystem
{
    [Dependency] private readonly INetManager _verbConnection = default!;

    private bool CanSendServerVerbsAquila(NetEntity target)
    {
        return _verbConnection.IsConnected && !target.IsClientSide();
    }
}
