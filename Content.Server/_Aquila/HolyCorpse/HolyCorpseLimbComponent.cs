using Content.Shared.Aquila.HolyCorpse;
using Robust.Shared.Prototypes;

namespace Content.Server.Aquila.HolyCorpse;

/// <summary>
/// Implantable limb. Stores potential sets (guardian + components);
/// one set is chosen at random upon initial implantation into a host.
/// </summary>
[RegisterComponent]
public sealed partial class HolyCorpseLimbComponent : Component
{
    [DataField(required: true)]
    public List<HolyCorpseSet> Sets = new();

    [DataField(required: true)]
    public EntProtoId FlashProto = default!;

    [DataField]
    public float ImplantDelay = 3f;
}
