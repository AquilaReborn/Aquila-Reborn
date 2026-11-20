using Robust.Shared.Audio;
using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
[Access(typeof(WOUParasiteModeSystem))]
public sealed partial class WOUParasiteModeComponent : Component
{
    [DataField] public string ParasiteName = "Wonder Of U";
    
    [DataField] public float WalkSpeed = 1.25f;
    [DataField] public float SprintSpeed = 2.25f;
    
    [DataField] public float FlashRange = 5f;
    [DataField] public TimeSpan FlashDuration = TimeSpan.FromSeconds(0.8);

    [DataField]
    public SoundSpecifier SwitchSound = new SoundPathSpecifier("/Audio/_Aquila/Effects/WOU/SwitchMode.ogg")
    {
        Params = AudioParams.Default.WithVolume(-2f).WithMaxDistance(10f)
    };

    [DataField] public EntProtoId BeeSphereAction = "WOUActionBeeSphere";
    [ViewVariables] public EntityUid? BeeSphereActionEntity;
    [DataField] public EntProtoId DoubleReflectAction = "WOUActionDoubleReflect";
    [ViewVariables] public EntityUid? DoubleReflectActionEntity;
    [DataField] public EntProtoId CapsuleSphereAction = "WOUActionCapsuleSphere";
    [ViewVariables] public EntityUid? CapsuleSphereActionEntity;
    [DataField] public EntProtoId SlipSphereAction = "WOUActionSlipSphere";
    [ViewVariables] public EntityUid? SlipSphereActionEntity;
}