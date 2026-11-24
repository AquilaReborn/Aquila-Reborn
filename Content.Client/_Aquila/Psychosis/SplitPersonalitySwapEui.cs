using Content.Client.Eui;
using Content.Shared._Aquila.Psychosis;
using JetBrains.Annotations;
using Robust.Client.Graphics;

namespace Content.Client._Aquila.Psychosis;

[UsedImplicitly]
public sealed class SplitPersonalitySwapEui : BaseEui
{
    [Dependency] private readonly IClyde _clyde = default!;

    private readonly SplitPersonalitySwapWindow _window;

    private bool _answered;

    public SplitPersonalitySwapEui()
    {
        IoCManager.InjectDependencies(this);

        _window = new SplitPersonalitySwapWindow();

        _window.AcceptButton.OnPressed += _ => Answer(true);
        _window.DenyButton.OnPressed += _ => Answer(false);
        _window.OnClose += () => Answer(false);
    }

    public override void Opened()
    {
        _clyde.RequestWindowAttention();
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
        SendMessage(new SplitPersonalitySwapAnswerMessage(accepted));
        _window.Close();
    }
}
