using System.Linq;
using Content.Shared.Body.Part;
using Content.Shared.Body.Systems;
using Content.Shared.Heretic.Components;
using Robust.Shared.Random;

namespace Content.Server.Heretic.EntitySystems;

public sealed partial class HereticBladeSystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void ApplyLockBladeEffect(EntityUid target, EntityUid performer, HereticBladeComponent? blade)
    {
        base.ApplyLockBladeEffect(target, performer, blade);

        if (blade == null)
            return;

        if (!_heretic.TryGetHereticComponent(performer, out var heretic, out _) || heretic.PathStage < 7)
            return;

        if (!_random.Prob(blade.LockOrganChance))
            return;

        var targetPart = _body.GetRandomBodyPart(target, (EntityUid?) performer);
        var (partType, symmetry) = _body.ConvertTargetBodyPart(targetPart);

        RemoveRandomOrgan(target, partType, symmetry);
    }

    private void RemoveRandomOrgan(EntityUid target, BodyPartType partType, BodyPartSymmetry symmetry)
    {
        foreach (var (partId, _) in _body.GetBodyChildrenOfType(target, partType, symmetry: symmetry))
        {
            if (FindOrganOrNull(partId) is not { } organ)
                continue;

            _body.TryRemoveOrgan(organ);
            return;
        }
    }

    private EntityUid? FindOrganOrNull(EntityUid part)
    {
        EntityUid? current = part;

        while (current is { } partId)
        {
            var organs = _body.GetPartOrgans(partId).ToList();
            if (organs.Count > 0)
                return _random.Pick(organs).Id;

            current = _body.GetParentPartOrNull(partId);
        }

        return null;
    }
}
