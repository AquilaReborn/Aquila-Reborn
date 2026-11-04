using Content.Shared._Shitmed.Medical.Surgery;

namespace Content.Server._Shitmed.Medical.Surgery;

public sealed partial class SurgerySystem
{
    private EntityUid? GetSelfSurgeon(EntityUid body)
    {
        return _ui.IsUiOpen(body, SurgeryUIKey.Key, body) ? body : null;
    }
}
