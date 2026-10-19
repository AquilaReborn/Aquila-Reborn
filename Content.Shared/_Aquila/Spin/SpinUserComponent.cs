using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Aquila.Spin;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpinUserComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? BoundEntity;

    [DataField, AutoNetworkedField]
    public int Charges = 0;

    [DataField, AutoNetworkedField]
    public int MaxCharges = 7;

    [DataField]
    public EntProtoId ActionProto = "ActionSpinSummon";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField]
    public SoundSpecifier? SummonSound;

    [DataField]
    public EntProtoId EmpowerActionProto = "ActionSpinEmpower";

    [DataField, AutoNetworkedField]
    public EntityUid? EmpowerAction;

    [DataField]
    public TimeSpan EmpowerDuration = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan NextChargeTime = TimeSpan.Zero;

    [DataField]
    public SoundSpecifier? ChargeSound = new SoundPathSpecifier("/Audio/_Aquila/Spin/PowerUpSoundEffects.ogg");

    [DataField]
    public EntProtoId EmpowerItemEffectProto = "SpinEmpowerEffect";

    [DataField]
    public EntProtoId HitEffectProto = "SpinHitEffect";

    [DataField]
    public EntProtoId EmpowerUserEffectProto = "SpinUserEmpowerEffect";
}
