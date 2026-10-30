using Content.Shared.Aquila.HolyCorpse.DataDefinitions;
using Robust.Shared.Prototypes;

namespace Content.Server.Aquila.HolyCorpse;

/// <summary>
/// Конечность святого трупа, которую можно вживить в существо.
/// </summary>
[RegisterComponent, Access(typeof(HolyCorpseSystem))]
public sealed partial class HolyCorpseLimbComponent : Component
{
    /// <summary>
    /// Возможные наборы, из которых при первом вживлении случайно выбирается один.
    /// </summary>
    [DataField(required: true)]
    public List<HolyCorpseSet> Sets = new();

    /// <summary>
    /// Эффект, который висит на цели во время вживления.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId FlashProto;

    /// <summary>
    /// Время вживления конечности.
    /// </summary>
    [DataField]
    public TimeSpan ImplantDelay = TimeSpan.FromSeconds(3);
}
