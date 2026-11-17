using Content.Server._Aquila.Psychosis.Components;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    private bool AquilaBlocksRadio(EntityUid source)
    {
        return HasComp<ImaginaryFriendComponent>(source)
            || HasComp<SplitPersonalityPassengerComponent>(source);
    }
}
