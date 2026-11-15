using Robust.Shared.Prototypes;
using Robust.Shared.Audio;

namespace Content.Server.Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteSlipSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 1f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
}

[RegisterComponent]
public sealed partial class WOUBeeSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 4f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int BeesSpawned;
    [DataField] public int MaxBees = 15;
    [DataField] public string BeeProto = "WOUBeeLicoxideAggressive";
}

[RegisterComponent]
public sealed partial class WOUCapsuleSphereComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
    [DataField] public float Radius = 2f;
    [ViewVariables] public HashSet<EntityUid> TriggeredEntities = new();
    [ViewVariables] public int CapsulesDropped;
    [DataField] public int MaxCapsules = 5;
    [DataField] public string CapsuleProto = "WOUCapsule";
}

[RegisterComponent]
public sealed partial class WOUHumanComponent : Component
{
    [ViewVariables] public EntityUid Partner;
    [ViewVariables] public EntityUid? ReturnActionEntity;
}

[RegisterComponent]
public sealed partial class WOUParasiteDoubleReflectComponent : Component
{
    [ViewVariables] public TimeSpan ExpiresAt;
}

[RegisterComponent]
public sealed partial class WOUParasiteHostComponent : Component
{
    [DataField] public EntProtoId ToggleFormAction = "WOUActionToggleForm";
    [ViewVariables] public EntityUid? ToggleFormActionEntity;
    [DataField] public EntProtoId BeeSphereAction = "WOUActionBeeSphere";
    [ViewVariables] public EntityUid? BeeSphereActionEntity;
    [DataField] public EntProtoId DoubleReflectAction = "WOUActionDoubleReflect";
    [ViewVariables] public EntityUid? DoubleReflectActionEntity;
    [DataField] public EntProtoId CapsuleSphereAction = "WOUActionCapsuleSphere";
    [ViewVariables] public EntityUid? CapsuleSphereActionEntity;
    [DataField] public EntProtoId SlipSphereAction = "WOUActionSlipSphere";
    [ViewVariables] public EntityUid? SlipSphereActionEntity;
}

[RegisterComponent]
public sealed partial class WOUParasiteModeComponent : Component
{
    [DataField] public bool StartInModeTwo;
    [ViewVariables] public bool IsModeTwo;
    [DataField] public string ModeOneName = "Satoru Akefu";
    [DataField] public string ModeTwoName = "Wonder Of U";
    [DataField] public float ModeOneWalkSpeed = 2.5f;
    [DataField] public float ModeOneSprintSpeed = 4.5f;
    [DataField] public float ModeTwoWalkSpeed = 1.25f;
    [DataField] public float ModeTwoSprintSpeed = 2.25f;
    [DataField] public float FlashRange = 5f;
    [DataField] public TimeSpan FlashDuration = TimeSpan.FromSeconds(0.8);

    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/_Aquila/Effects/WOU/SwitchMode.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(10f)
    };

    [DataField] public EntProtoId ToggleFormAction = "WOUActionToggleForm";
    [ViewVariables] public EntityUid? ToggleFormActionEntity;
    [DataField] public EntProtoId BeeSphereAction = "WOUActionBeeSphere";
    [ViewVariables] public EntityUid? BeeSphereActionEntity;
    [DataField] public EntProtoId DoubleReflectAction = "WOUActionDoubleReflect";
    [ViewVariables] public EntityUid? DoubleReflectActionEntity;
    [DataField] public EntProtoId CapsuleSphereAction = "WOUActionCapsuleSphere";
    [ViewVariables] public EntityUid? CapsuleSphereActionEntity;
    [DataField] public EntProtoId SlipSphereAction = "WOUActionSlipSphere";
    [ViewVariables] public EntityUid? SlipSphereActionEntity;
}