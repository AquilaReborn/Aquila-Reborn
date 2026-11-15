using Content.Shared.Examine;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Network;

namespace Content.Server.Examine;

public sealed partial class ExamineSystem
{
    [Dependency] private readonly MobStateSystem _examineMobState = default!;

    private bool TryBlockExamineAquila(EntityUid examiner, EntityUid examined, NetEntity netEntity, int id,
        INetChannel channel)
    {
        if (!IsExamineBlockedAquila(examiner, examined))
            return false;

        RaiseNetworkEvent(new ExamineSystemMessages.ExamineInfoResponseMessage(
            netEntity, id, _entityOutOfRangeMessage, knowTarget: false), channel);
        return true;
    }

    private bool IsExamineBlockedAquila(EntityUid examiner, EntityUid examined)
    {
        if (!HasComp<MobStateComponent>(examined))
            return false;

        return _examineMobState.IsCritical(examiner) || _examineMobState.IsDead(examiner);
    }
}
