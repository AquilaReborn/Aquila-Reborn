using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.HolyCorpse;

/// <summary>
/// Set: guardian + components that the host receives upon their first implantation
/// </summary>
[DataDefinition]
public sealed partial class HolyCorpseSet
{
    [DataField(required: true)]
    public EntProtoId GuardianProto = default!;

    [DataField]
    public ComponentRegistry? Components;
}
