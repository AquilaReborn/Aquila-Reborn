using Content.Shared._Aquila.Economy.Systems;
using Content.Shared.Access;
using Content.Shared.Cargo.Prototypes;
using Content.Shared.Contraband;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Economy.Components;

[RegisterComponent, NetworkedComponent, Access(typeof(SharedPaidVendingMachineSystem))]
public sealed partial class PaidVendingMachineComponent : Component
{
    /// <summary>
    /// Множитель цен категорий для этого автомата.
    /// </summary>
    [DataField]
    public float PriceMultiplier = 1f;

    /// <summary>
    /// Цены, заданные вручную вместо цены категории.
    /// </summary>
    [DataField]
    public Dictionary<EntProtoId, int> PriceOverrides = new();

    /// <summary>
    /// Превращает ограничение доступа автомата в бесплатность: с доступом товары бесплатные, без доступа их можно купить.
    /// </summary>
    [DataField]
    public bool FreeWithAccess = true;

    /// <summary>
    /// Доступы, с любым из которых товары бесплатные. Подходит для автоматов без ограничения доступа.
    /// </summary>
    [DataField]
    public List<ProtoId<AccessLevelPrototype>> FreeAccess = new();

    /// <summary>
    /// Уровни контрабанды, которые из обычного ассортимента продаются только тем, кому они разрешены, или с доступом к автомату.
    /// </summary>
    [DataField]
    public List<ProtoId<ContrabandSeverityPrototype>> RestrictedSeverities = new() { "Restricted", "Major", "GrandTheft", "HighlyIllegal", "Syndicate" };

    /// <summary>
    /// Доля оплаты, которая идёт в бюджет станции. Остальное уходит ЦК.
    /// </summary>
    [DataField]
    public float RevenueShare = 0.2f;

    /// <summary>
    /// Счёт бюджета станции, куда идёт выручка.
    /// </summary>
    [DataField]
    public ProtoId<CargoAccountPrototype> RevenueAccount = "Service";

    /// <summary>
    /// Направлять долю дохода в бюджет отдела автомата по названию отдела в его доступе, если отдел удалось определить; иначе используется <see cref="RevenueAccount"/>.
    /// </summary>
    [DataField]
    public bool RevenueByDepartment = true;

    private static readonly ProtoId<AccessLevelPrototype> SecurityAccess = "Security";
    private static readonly ProtoId<AccessLevelPrototype> MedicalAccess = "Medical";
    private static readonly ProtoId<AccessLevelPrototype> EngineeringAccess = "Engineering";
    private static readonly ProtoId<AccessLevelPrototype> ResearchAccess = "Research";
    private static readonly ProtoId<AccessLevelPrototype> CargoAccess = "Cargo";
    private static readonly ProtoId<AccessLevelPrototype> QuartermasterAccess = "Quartermaster";
    private static readonly ProtoId<AccessLevelPrototype> SalvageAccess = "Salvage";
    private static readonly ProtoId<AccessLevelPrototype> ServiceAccess = "Service";
    private static readonly ProtoId<AccessLevelPrototype> BarAccess = "Bar";
    private static readonly ProtoId<AccessLevelPrototype> KitchenAccess = "Kitchen";
    private static readonly ProtoId<AccessLevelPrototype> HydroponicsAccess = "Hydroponics";
    private static readonly ProtoId<AccessLevelPrototype> JanitorAccess = "Janitor";
    private static readonly ProtoId<AccessLevelPrototype> HeadOfPersonnelAccess = "HeadOfPersonnel";

    private static readonly ProtoId<CargoAccountPrototype> CargoAccount = "Cargo";
    private static readonly ProtoId<CargoAccountPrototype> EngineeringAccount = "Engineering";
    private static readonly ProtoId<CargoAccountPrototype> MedicalAccount = "Medical";
    private static readonly ProtoId<CargoAccountPrototype> ScienceAccount = "Science";
    private static readonly ProtoId<CargoAccountPrototype> SecurityAccount = "Security";
    private static readonly ProtoId<CargoAccountPrototype> ServiceAccount = "Service";

    /// <summary>
    /// Бюджеты отделов, в которые идёт выручка автомата: бюджет выбирается по доступу из замка автомата.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<AccessLevelPrototype>, ProtoId<CargoAccountPrototype>> DepartmentAccounts = new()
    {
        { SecurityAccess, SecurityAccount },
        { MedicalAccess, MedicalAccount },
        { EngineeringAccess, EngineeringAccount },
        { ResearchAccess, ScienceAccount },
        { CargoAccess, CargoAccount },
        { QuartermasterAccess, CargoAccount },
        { SalvageAccess, CargoAccount },
        { ServiceAccess, ServiceAccount },
        { BarAccess, ServiceAccount },
        { KitchenAccess, ServiceAccount },
        { HydroponicsAccess, ServiceAccount },
        { JanitorAccess, ServiceAccount },
        { HeadOfPersonnelAccess, ServiceAccount },
    };

    /// <summary>
    /// Описание покупки в истории операций.
    /// </summary>
    [DataField]
    public LocId PurchaseReason = "bank-transaction-purchase";

    /// <summary>
    /// Звук успешной оплаты, который слышит покупатель.
    /// </summary>
    [DataField]
    public SoundSpecifier? PurchaseSound = new SoundPathSpecifier("/Audio/_Aquila/Economy/terminal_success.ogg", AudioParams.Default.WithVolume(-7f));

    /// <summary>
    /// До какого момента покупка в этом автомате не спрашивает PIN повторно. Ключ - покупатель, значение - конец сессии.
    /// </summary>
    public Dictionary<EntityUid, TimeSpan> PinSessions = new();
}
