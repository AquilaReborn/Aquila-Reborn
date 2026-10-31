using Content.Server.EUI;
using Content.Shared._Aquila.Psychosis;
using Content.Shared.Eui;

namespace Content.Server._Aquila.Psychosis;

public sealed class SplitPersonalitySwapEui(SplitPersonalitySystem system, EntityUid host) : BaseEui
{
    private bool _answered;

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not SplitPersonalitySwapAnswerMessage answer || _answered)
            return;

        _answered = true;
        system.AnswerRequest(host, answer.Accepted);
        Close();
    }

    public override void Closed()
    {
        base.Closed();

        if (!_answered)
            system.AnswerRequest(host, false);
    }
}
