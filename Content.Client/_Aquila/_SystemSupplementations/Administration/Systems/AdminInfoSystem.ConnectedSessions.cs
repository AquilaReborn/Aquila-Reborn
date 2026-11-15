using Robust.Shared.Network;

namespace Content.Client.Administration.Systems;

public sealed partial class AdminInfoSystem
{
    [Dependency] private readonly INetManager _connection = default!;

    private void InitializeAquila()
    {
        if (_connection.IsConnected)
            b();
    }
}
