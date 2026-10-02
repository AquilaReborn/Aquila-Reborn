using Content.Shared.Chemistry.Reagent;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.AutoCook;

[RegisterComponent]
public sealed partial class AutoCookerComponent : Component
{
    public const string BeakerSlotName = "autoCookBeakerSlot";

    [DataField(required: true)]
    public AutoCookKind Kind;

    [DataField]
    public List<ProtoId<ReagentPrototype>> BaseReagents = new();

    [DataField]
    public List<ProtoId<ReagentPrototype>> ExcludedReagents = new();

    [DataField]
    public List<string> BaseMetabolisms = new();

    [DataField]
    public List<ProtoId<ReagentPrototype>> ExtraTargets = new();

    [DataField]
    public List<string> TargetGroups = new();

    [DataField]
    public List<string> TargetMetabolisms = new();

    [DataField]
    public List<string> AllowedMixing = new();

    [DataField]
    public int MaxQueue = 5;

    [DataField]
    public ProtoId<SinkPortPrototype> RepeatPort = "AutoCookRepeat";

    [DataField]
    public float WorkingLoad = 400f;

    [DataField]
    public float DoneSeconds = 3f;

    [DataField]
    public float SynthesisSeconds = 1.5f;

    [DataField]
    public float SecondsPerUnit = 0.04f;

    [DataField]
    public float ReactionSeconds = 3f;

    [DataField]
    public float PrepareSeconds = 2f;

    [DataField]
    public string StorageContainer = "storagebase";

    [DataField]
    public string PantrySolution = "pantry";

    [DataField]
    public ItemSlot BeakerSlot = new();

    [DataField]
    public SoundSpecifier StartSound = new SoundPathSpecifier("/Audio/Machines/machine_switch.ogg");

    [DataField]
    public SoundSpecifier StepSound = new SoundPathSpecifier("/Audio/Machines/button.ogg");

    [DataField]
    public SoundSpecifier DoneSound = new SoundPathSpecifier("/Audio/Machines/microwave_done_beep.ogg");

    public AutoCookJob? Job;

    public AutoCookOrder? LastOrder;

    public TimeSpan DoneUntil;

    public AutoCookerVisualState? Visual;

    public readonly List<AutoCookOrder> Queue = new();
}
