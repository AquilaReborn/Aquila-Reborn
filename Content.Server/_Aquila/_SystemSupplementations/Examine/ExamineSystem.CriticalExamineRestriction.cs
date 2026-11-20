using Content.Shared.Examine;
using Robust.Shared.Network;

namespace Content.Server.Examine;

public sealed partial class ExamineSystem
{
    private bool TryBlockExamineAquila(EntityUid examiner, EntityUid examined, NetEntity netEntity, int id,
        INetChannel channel)
    {
        if (!IsExamineBlockedAquila(examiner, examined))
            return false;

        RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
            netEntity, id, _entityOutOfRangeMessage, knowTarget: false), channel);
        return true;
    }
}
