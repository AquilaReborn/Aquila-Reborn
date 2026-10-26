using Content.Goobstation.Shared._Aquila.Changeling;
using Content.Server.EUI;
using Content.Shared.Eui;

namespace Content.Goobstation.Server._Aquila.Changeling;

public sealed class ChangelingHostControlEui(ChangelingHostControlSystem system, EntityUid host, EntityUid victimMind) : BaseEui
{
    private bool _answered;

    public override void HandleMessage(EuiMessageBase msg)
    {
        base.HandleMessage(msg);

        if (msg is not ChangelingHostControlRequestMessage request || _answered)
            return;

        _answered = true;
        system.AnswerRequest(host, victimMind, request.Accepted);
        Close();
    }

    public override void Closed()
    {
        base.Closed();

        if (!_answered)
            system.AnswerRequest(host, victimMind, false);
    }
}
