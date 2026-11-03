using Content.Shared._Aquila.Cyberware;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Cyberware;

[UsedImplicitly]
public sealed class CyberwareScannerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private CyberwareScannerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<CyberwareScannerWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is CyberwareScannerUiState cast)
            _window?.UpdateState(cast);
    }
}
