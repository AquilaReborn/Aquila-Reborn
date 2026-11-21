using Robust.Shared.GameObjects;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.WOU.Stand;

[RegisterComponent]
public sealed partial class WOUParasiteHostComponent : Component
{
    [DataField] public EntProtoId BeeSphereAction = "WOUActionBeeSphere";
    [ViewVariables] public EntityUid? BeeSphereActionEntity;
    [DataField] public EntProtoId DoubleReflectAction = "WOUActionDoubleReflect";
    [ViewVariables] public EntityUid? DoubleReflectActionEntity;
    [DataField] public EntProtoId CapsuleSphereAction = "WOUActionCapsuleSphere";
    [ViewVariables] public EntityUid? CapsuleSphereActionEntity;
    [DataField] public EntProtoId SlipSphereAction = "WOUActionSlipSphere";
    [ViewVariables] public EntityUid? SlipSphereActionEntity;
}