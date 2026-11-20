using Content.Server._Aquila.ProfileImage.Components;
using Content.Shared._Aquila.Images.Helpers;
using Content.Shared.DetailExaminable;

namespace Content.Server.Station.Systems;

public sealed partial class StationSpawningSystem
{
    private void ApplyProfileImage(EntityUid uid, byte[]? png)
    {
        if (!PngHelper.TryValidateProfileImage(png))
            return;

        EnsureComp<ProfileImageComponent>(uid).Png = png;
        EnsureComp<DetailExaminableComponent>(uid);
    }
}
