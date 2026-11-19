using Content.Client._Aquila.Changeling.Types;
using Content.Client.UserInterface.Controls;
using Content.Goobstation.Shared._Aquila.Changeling.Types;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Changeling;

[UsedImplicitly]
public sealed class ChangelingTransformBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    private ChangelingTransformMenuSession? _session;

    protected override void Open()
    {
        if (_session != null)
            return;

        base.Open();

        var menu = this.CreateWindow<SimpleRadialMenu>();
        _session = new ChangelingTransformMenuSession(menu, OnFormSelected);
        menu.OnClose += OnMenuClosed;
        Update<ChangelingTransformState>();
        menu.OpenOverMouseScreenPosition();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (_session != null && state is ChangelingTransformState cast)
            _ui.GetUIController<ChangelingTransformUIController>().UpdateMenu(_session, cast);
    }

    private void OnFormSelected(int index)
    {
        SendMessage(new ChangelingTransformIdentitySelectMessage(index));
    }

    private void OnMenuClosed()
    {
        if (_session == null)
            return;

        _session.Menu.OnClose -= OnMenuClosed;
        _ui.GetUIController<ChangelingTransformUIController>().ClearMenu(_session);
        _session = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            OnMenuClosed();

        base.Dispose(disposing);
    }
}
