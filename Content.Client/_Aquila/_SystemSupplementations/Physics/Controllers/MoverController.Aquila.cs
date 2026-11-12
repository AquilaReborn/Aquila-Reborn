using Content.Shared.CCVar;
using Content.Shared.Movement.Events;
using Robust.Shared.Network;

namespace Content.Client.PhysicsSystem.Controllers;

public sealed partial class MoverController
{
    [Dependency] private readonly INetManager _connection = default!;

    private void InitializeAquila()
    {
        Subs.CVar(_cfg, CCVars.DefaultWalk, OnDefaultWalkChangedAquila);
    }

    private void OnDefaultWalkChangedAquila(bool _)
    {
        if (_connection.IsConnected)
            RaiseNetworkEvent(new UpdateInputCVarsMessage());
    }
}
