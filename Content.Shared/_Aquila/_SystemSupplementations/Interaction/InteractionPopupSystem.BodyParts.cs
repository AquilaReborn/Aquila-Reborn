using Content.Shared._Aquila.Targeting.Systems;
using Content.Shared._Shitmed.Targeting;

namespace Content.Shared.Interaction;

public sealed partial class InteractionPopupSystem
{
    [Dependency] private readonly BodyPartTargetingSystem _bodyPartTargeting = default!;

    private string? GetBodyPartString(Dictionary<TargetBodyPart, string> map, EntityUid user)
    {
        if (map.Count == 0)
            return null;

        return _bodyPartTargeting.TryGetTargetedBodyPart(user, out var part)
            && part is { } bodyPart
            && map.TryGetValue(bodyPart, out var locId)
            ? locId
            : null;
    }
}
