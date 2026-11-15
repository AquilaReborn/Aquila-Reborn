using System.Numerics;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Preferences;

namespace Content.Shared.Humanoid;

public abstract partial class SharedHumanoidAppearanceSystem
{
    private void ApplyProfileScale(EntityUid uid, HumanoidCharacterProfile profile, SpeciesPrototype species, HumanoidAppearanceComponent humanoid)
    {
        var scale = profile.Height <= 0 || profile.Width <= 0
            ? new Vector2(species.DefaultWidth, species.DefaultHeight)
            : new Vector2(profile.Width, profile.Height);

        SetScale(uid, scale, true, humanoid);
    }
}
