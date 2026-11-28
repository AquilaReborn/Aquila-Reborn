using System.Diagnostics.CodeAnalysis;
using Content.Shared.Body.Part;

namespace Content.Goobstation.Client.MantisBlades;

public sealed partial class MantisBladeVisualsSystem
{
    private bool TryGetBladePart(EntityUid blade, [NotNullWhen(true)] out BodyPartComponent? part)
    {
        part = null;
        if (blade == EntityUid.Invalid || Deleted(blade) || !TryComp<TransformComponent>(blade, out var bladeTransform))
            return false;
        return TryComp<BodyPartComponent>(bladeTransform.ParentUid, out part);
    }
}
