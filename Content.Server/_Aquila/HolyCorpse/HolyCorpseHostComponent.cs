using Robust.Shared.Containers;
using Robust.Shared.Prototypes;

namespace Content.Server.Aquila.HolyCorpse;

/// <summary>
/// Существо, в которое вживлена хотя бы одна конечность святого трупа.
/// </summary>
[RegisterComponent, Access(typeof(HolyCorpseSystem))]
public sealed partial class HolyCorpseHostComponent : Component
{
    /// <summary>
    /// ID контейнера с вживлёнными конечностями.
    /// </summary>
    [DataField]
    public string ContainerId = "HolyCorpseLimbContainer";

    /// <summary>
    /// Время извлечения всех конечностей.
    /// </summary>
    [DataField]
    public TimeSpan ExtractDelay = TimeSpan.FromSeconds(7);

    /// <summary>
    /// Прибавка к множителю стража за каждую конечность после первой.
    /// </summary>
    [DataField]
    public float BonusPerExtraLimb = 0.1f;

    [ViewVariables]
    public Container LimbContainer = default!;

    [ViewVariables]
    public List<EntityUid> Limbs = new();

    [ViewVariables]
    public ComponentRegistry? AppliedComponents;

    [ViewVariables]
    public float? BaseDistanceAllowed;

    [ViewVariables]
    public float? BaseRechargeCooldown;
}
