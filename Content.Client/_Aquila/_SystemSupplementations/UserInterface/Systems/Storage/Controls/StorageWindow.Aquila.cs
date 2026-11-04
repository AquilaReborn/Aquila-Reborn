using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Storage.Controls;

public sealed partial class StorageWindow
{
    private Control? _layoutParent;

    protected override void EnteredTree()
    {
        base.EnteredTree();

        if (Parent is null or LayoutContainer)
            return;

        _layoutParent = Parent;
        _layoutParent.OnResized += OnLayoutParentResized;
    }

    protected override void ExitedTree()
    {
        if (_layoutParent != null)
            _layoutParent.OnResized -= OnLayoutParentResized;

        _layoutParent = null;
        base.ExitedTree();
    }

    private void OnLayoutParentResized()
    {
        InvalidateArrange();
    }
}
