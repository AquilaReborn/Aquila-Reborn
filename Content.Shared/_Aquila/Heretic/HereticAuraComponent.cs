using Content.Shared._Shitcode.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.Heretic;

/// <summary>
/// Видимая всем аура еретика, которая включается после набора достаточного количества очков знаний.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticAuraComponent : BaseSpriteOverlayComponent
{
    /// <summary>
    /// Спрайт ауры.
    /// </summary>
    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Aquila/Heretic/Effects/heretic_aura.rsi"), "heretic_aura");

    /// <summary>
    /// Ключ слоя ауры в спрайте сущности.
    /// </summary>
    public override Enum Key { get; set; } = HereticAuraKey.Key;
}
