using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Aquila.HolyCorpse;

[RegisterComponent]
public sealed partial class HolyCorpseCreatorComponent : Component
{
    public bool Used = false;

    [DataField("guardianProto", customTypeSerializer: typeof(PrototypeIdSerializer<EntityPrototype>), required: true)]
    public string GuardianProto { get; set; } = default!;

    [DataField(required: true)]
    public EntProtoId FlashProto = default!;

    [DataField("delay")]
    public float InjectionDelay = 1.85f;

    [DataField]
    public ComponentRegistry? Components;
}
