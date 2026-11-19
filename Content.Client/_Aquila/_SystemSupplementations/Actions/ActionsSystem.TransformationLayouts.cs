using Content.Client.UserInterface.Systems.Actions;
using Content.Shared._Aquila.Actions.Types;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.Actions;

public sealed partial class ActionsSystem
{
    [Dependency] private readonly IUserInterfaceManager _transformationUi = default!;

    private void InitializeTransformationLayouts()
    {
        SubscribeNetworkEvent<TransformActionLayoutEvent>(OnTransformActionLayout);
    }

    private void OnTransformActionLayout(TransformActionLayoutEvent args, EntitySessionEventArgs session)
    {
        if (session.SenderSession != _playerManager.LocalSession)
            return;

        _transformationUi.GetUIController<ActionUIController>().LoadTransformationLayout(args);
    }
}
