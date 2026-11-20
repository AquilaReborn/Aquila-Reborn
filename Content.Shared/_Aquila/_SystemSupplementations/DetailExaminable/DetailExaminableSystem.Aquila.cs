using Content.Shared._Aquila.ProfileImage.Events;

namespace Content.Shared.DetailExaminable;

public sealed partial class DetailExaminableSystem
{
    private void RaiseDetailsExamineAquila(EntityUid target)
    {
        RaiseLocalEvent(new DetailsExamineOpenedEvent(target));
    }
}
