using Content.Client.UserInterface.Controls;
using Content.Shared._Aquila.ScreenNoise;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._Aquila.ScreenNoise;

public sealed class ScreenNoiseUiSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IUserInterfaceManager _uiManager = default!;
    [Dependency] private readonly UserInterfaceSystem _ui = default!;

    private readonly HashSet<BoundUserInterface> _decorated = new();

    public override void Initialize()
    {
        base.Initialize();

        _uiManager.WindowRoot.OnChildAdded += OnWindowAdded;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _uiManager.WindowRoot.OnChildAdded -= OnWindowAdded;
        _decorated.Clear();
    }

    private void OnWindowAdded(Control control)
    {
        if (control is not BaseWindow window)
            return;

        _decorated.RemoveWhere(bui => !bui.IsOpened);

        if (!TryComp<UserInterfaceUserComponent>(_player.LocalEntity, out var user))
            return;

        foreach (var (uid, keys) in user.OpenInterfaces)
        {
            if (!HasComp<ScreenNoiseUiComponent>(uid))
                continue;

            foreach (var key in keys)
            {
                if (!_ui.TryGetOpenUi(uid, key, out var bui) || !_decorated.Add(bui))
                    continue;

                AddNoise(window);
                return;
            }
        }
    }

    private static void AddNoise(BaseWindow window)
    {
        var container = window switch
        {
            DefaultWindow defaultWindow => defaultWindow.Contents,
            FancyWindow fancyWindow => fancyWindow.ContentsContainer,
            _ => window,
        };

        container.AddChild(new ScreenNoise());
    }
}
