using Content.Server._Aquila.Heretic.Conjurations.Systems;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._Aquila.Heretic.Conjurations.Components;

/// <summary>
/// Зубастый тигель: копит массу, жрёт органы и части тел, переключает якорь кодексом или хваткой Мансуса
/// и варит зелья, когда масса набрана полностью.
/// </summary>
[RegisterComponent, AutoGenerateComponentPause, Access(typeof(MawedCrucibleSystem))]
public sealed partial class MawedCrucibleComponent : Component
{
    /// <summary>
    /// Максимальный запас массы тигля.
    /// </summary>
    [DataField]
    public int MaxMass = 3;

    /// <summary>
    /// Запас массы при появлении тигля.
    /// </summary>
    [DataField]
    public int StartMass = 3;

    /// <summary>
    /// Текущий запас массы. Полный запас нужен для варки одного зелья.
    /// </summary>
    [DataField]
    public int Mass;

    /// <summary>
    /// Через сколько тигель сам добавляет единицу массы.
    /// </summary>
    [DataField]
    public TimeSpan RefillInterval = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Момент следующего пассивного пополнения массы.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextRefill;

    /// <summary>
    /// Зелья, которые тигель может сварить при полной массе.
    /// </summary>
    [DataField]
    public List<EntProtoId> Brews = new()
    {
        "MawedCrucibleBrewSoul",
        "MawedCrucibleBrewDusk",
        "MawedCrucibleBrewWounded",
    };

    /// <summary>
    /// Состояние слоя спрайта для заполненного тигля.
    /// </summary>
    [DataField]
    public string FullState = "crucible";

    /// <summary>
    /// Состояние слоя спрайта для пустого тигля.
    /// </summary>
    [DataField]
    public string EmptyState = "crucible_empty";

    /// <summary>
    /// Звук пассивного пополнения массы.
    /// </summary>
    [DataField]
    public SoundSpecifier RefillSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/eatfood.ogg");

    /// <summary>
    /// Звук поедания органа или части тела.
    /// </summary>
    [DataField]
    public SoundSpecifier FeedSound = new SoundPathSpecifier("/Audio/_Goobstation/Heretic/eatfood.ogg");

    /// <summary>
    /// Звук переключения якоря.
    /// </summary>
    [DataField]
    public SoundSpecifier AnchorSound = new SoundPathSpecifier("/Audio/Items/deconstruct.ogg");

    /// <summary>
    /// Звук варки зелья.
    /// </summary>
    [DataField]
    public SoundSpecifier BrewSound = new SoundPathSpecifier("/Audio/Effects/desecration-02.ogg");

    /// <summary>
    /// Урон, который тигель наносит не-еретику за попытку взаимодействия.
    /// </summary>
    [DataField]
    public DamageSpecifier BiteDamage = new()
    {
        DamageDict =
        {
            { "Piercing", 5 },
        },
    };
}
