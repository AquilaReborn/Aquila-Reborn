using Robust.Shared.GameStates;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Numerics;
using Robust.Shared.Utility;

namespace Content.Shared.Aquila.Vehicles;

public enum GallopState : byte
{
    Idle,
    Waiting,
    WindowOpen,
    Slowed,
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class VehicleGallopComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public EntityUid? GallopAction;

    [ViewVariables, AutoNetworkedField]
    public EntityUid? Rider;

    [DataField]
    public int MaxStreak = 5;

    [DataField]
    public float MaxMultiplier = 1.5f;

    [DataField]
    public float StaminaCost = 30f;

    [DataField]
    public TimeSpan FirstDecayDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan NextDecayDelay = TimeSpan.FromSeconds(2);

    [ViewVariables, AutoNetworkedField]
    public int Streak;

    [ViewVariables, AutoNetworkedField]
    public float CurrentMultiplier = 1f;

    [ViewVariables, AutoNetworkedField]
    public TimeSpan NextDecayTime;

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

    [DataField]
    public float GaugeHorizontalOffset = -1f;

    [DataField]
    public float GaugeVerticalOffset = 0f;

    [DataField]
    public float GaugeScreenScale = 1f;

    [DataField]
    public float StaminaGaugeSpacing = 0.5f;

    [DataField]
    public Vector2 CloudOffset = new(0f, -0.55f);

    [DataField]
    public EntProtoId GallopActionProto = "ActionGallop";

    [DataField]
    public ResPath StreakGaugeRsi = new("/Textures/_Aquila/Other/Gallop/gallop_gauge.rsi");

    [DataField]
    public string StreakGaugeStatePrefix = "gallop_";

    [DataField]
    public ResPath StaminaGaugeRsi = new("/Textures/_Aquila/Other/Gallop/stamina_gauge.rsi");

    [DataField]
    public string StaminaGaugeStatePrefix = "stamina_";

    [DataField]
    public int StaminaDivisions = 10;
}

public sealed partial class GallopActionEvent : InstantActionEvent;

public sealed class VehicleGallopSuccessEvent : EntityEventArgs
{
    public readonly EntityUid Rider;

    public VehicleGallopSuccessEvent(EntityUid rider)
    {
        Rider = rider;
    }
}
