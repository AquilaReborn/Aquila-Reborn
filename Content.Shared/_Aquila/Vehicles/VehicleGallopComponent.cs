using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Aquila.Vehicles;

public enum GallopState : byte
{
    Idle,
    Waiting,
    WindowOpen,
    Slowed,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class VehicleGallopComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? GallopAction;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Rider;

    [ViewVariables]
    public DoAfterId? ActiveDoAfter;

    [DataField]
    public float Interval = 3f;

    [DataField]
    public float WindowLength = 1f;

    [DataField]
    public float IntervalStepPerStreak = 0.3f;

    [DataField]
    public float WindowLengthStepPerStreak = 0.1f;

    [DataField]
    public float MinInterval = 1f;

    [DataField]
    public float MinWindowLength = 0.4f;

    [DataField]
    public float SlowdownDuration = 3f;

    [DataField]
    public float SlowdownMultiplier = 0.7f;

    [DataField]
    public float MaxMultiplier = 1.5f;

    [DataField]
    public int MaxStreak = 5;

    [ViewVariables, DataField, AutoNetworkedField]
    public int Streak;

    [ViewVariables, DataField, AutoNetworkedField]
    public float CurrentMultiplier = 1f;

    [ViewVariables, AutoNetworkedField]
    public GallopState State = GallopState.Idle;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan WindowEnd;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan SlowEnd;

    [DataField]
    public SoundSpecifier? GallopSuccessSound;

    [DataField]
    public EntProtoId? SuccessCloudEffect;

    [DataField]
    public EntProtoId? StepCloudEffect;

    [DataField]
    public TimeSpan TimeBetweenSteps = TimeSpan.FromSeconds(0.3);

    [ViewVariables]
    public TimeSpan LastStep;
}

public sealed partial class GallopActionEvent : InstantActionEvent;

[Serializable, NetSerializable]
public sealed partial class GallopDoAfterEvent : SimpleDoAfterEvent
{
}

public sealed class VehicleGallopSuccessEvent : EntityEventArgs
{
    public readonly EntityUid Rider;

    public VehicleGallopSuccessEvent(EntityUid rider)
    {
        Rider = rider;
    }
}
