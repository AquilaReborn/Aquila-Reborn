using Content.Client.Eui;
using Content.Goobstation.Shared._Aquila.Changeling;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Goobstation.Client._Aquila.Changeling;

[UsedImplicitly]
public sealed class ChangelingHostControlEui : BaseEui
{
    private readonly ChangelingHostControlWindow _window;

    private bool _answered;

    public ChangelingHostControlEui()
    {
        _window = new ChangelingHostControlWindow();

        _window.AcceptButton.OnPressed += _ => Answer(true);
        _window.DenyButton.OnPressed += _ => Answer(false);
        _window.OnClose += () => Answer(false);
    }

    public override void Opened()
    {
        IoCManager.Resolve<IClyde>().RequestWindowAttention();
        _window.OpenCentered();
    }

    public override void Closed()
    {
        base.Closed();

        _answered = true;
        _window.Close();
    }

    private void Answer(bool accepted)
    {
        if (_answered)
            return;

        _answered = true;
        SendMessage(new ChangelingHostControlRequestMessage(accepted));
        _window.Close();
    }
}
