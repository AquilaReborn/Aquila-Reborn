using Robust.Shared.Network;

namespace Content.Shared.Body.Systems;

public abstract partial class SharedBloodstreamSystem
{
    [Dependency] private readonly INetManager _net = default!;
}
