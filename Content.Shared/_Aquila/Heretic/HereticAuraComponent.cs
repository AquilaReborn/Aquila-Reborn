using System.Numerics;
using Content.Shared._Shitcode.Heretic.SpriteOverlay;
using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.Heretic;

/// <summary>
/// Visible to everyone heretic aura that turns on after gaining enough knowledge points.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HereticAuraComponent : BaseSpriteOverlayComponent
{
    /// <summary>
    /// Aura sprite.
    /// </summary>
    [DataField]
    public override SpriteSpecifier? Sprite { get; set; } =
        new SpriteSpecifier.Rsi(new ResPath("_Aquila/Heretic/Effects/heretic_aura.rsi"), "heretic_aura");

    /// <summary>
    /// Aura layer key in the entity sprite.
    /// </summary>
    public override Enum Key { get; set; } = HereticAuraKey.Key;

    /// <summary>
    /// Aura sprite offset relative to the body: two pixels lower.
    /// </summary>
    [DataField]
    public override Vector2 Offset { get; set; } = new Vector2(0f, -2f / 32f);
}
