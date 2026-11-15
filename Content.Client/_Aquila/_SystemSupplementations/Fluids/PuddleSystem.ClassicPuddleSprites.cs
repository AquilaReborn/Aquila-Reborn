using Content.Client.IconSmoothing;
using Content.Shared.Fluids;
using Robust.Client.GameObjects;

namespace Content.Client.Fluids;

public sealed partial class PuddleSystem
{
    [Dependency] private readonly IconSmoothSystem _smooth = default!;

    private void UpdateClassicPuddleSprite(Entity<SpriteComponent> ent, float volume, IReadOnlyDictionary<Enum, object> appearanceData)
    {
        if (TryComp<IconSmoothComponent>(ent, out var smooth))
        {
            if (volume < LowThreshold)
            {
                _sprite.LayerSetRsiState(ent.AsNullable(), 0, $"{smooth.StateBase}a");
                _smooth.SetEnabled(ent, false, smooth);
            }
            else if (volume < MediumThreshold)
            {
                _sprite.LayerSetRsiState(ent.AsNullable(), 0, $"{smooth.StateBase}b");
                _smooth.SetEnabled(ent, false, smooth);
            }
            else if (!smooth.Enabled)
            {
                _sprite.LayerSetRsiState(ent.AsNullable(), 0, $"{smooth.StateBase}0");
                _smooth.SetEnabled(ent, true, smooth);
                _smooth.DirtyNeighbours(ent);
            }
        }

        if (appearanceData.TryGetValue(PuddleVisuals.SolutionColor, out var colorObj))
            _sprite.SetColor(ent.AsNullable(), (Color) colorObj);
    }
}
