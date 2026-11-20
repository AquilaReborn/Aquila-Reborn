using System.Numerics;
using Content.Client._Aquila.Images.Systems;
using Content.Shared._Aquila.Images.Helpers;
using Content.Shared._Aquila.Images.Types;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private const int ProfileImagePreviewSize = 256;
    private const int BytesPerKilobyte = 1024;

    private static readonly LocId ProfileImageHintLoc = "character-profile-image-hint";
    private static readonly LocId ProfileImageSizeErrorLoc = "character-profile-image-size-error";

    private bool _profileImageBusy;

    private void InitializeProfileImage()
    {
        ProfileImagePanel.MinSize = new Vector2(ProfileImagePreviewSize, ProfileImagePreviewSize);

        ProfileImageImportButton.OnPressed += _ => ImportProfileImage();
        ProfileImageExportButton.OnPressed += _ => ExportProfileImage();
        ProfileImageDeleteButton.OnPressed += _ => RemoveProfileImage();

        UpdateProfileImage();
    }

    private async void ImportProfileImage()
    {
        if (_profileImageBusy || Profile == null)
            return;

        _profileImageBusy = true;
        try
        {
            var png = await _entManager.System<ImageTransferSystem>().ImportPngAsync(ImageLimits.Profile);
            if (png == null)
                return;

            if (!PngHelper.TryGetPngDimensions(png, out var width, out var height)
                || !ImageLimits.Profile.AllowsDimension(width, height))
            {
                ProfileImageHint.Text = Loc.GetString(ProfileImageSizeErrorLoc,
                    ("size", ImageLimits.Profile.MaxDimension));
                return;
            }

            var profile = Profile;
            if (profile == null)
                return;

            Profile = profile.WithProfileImage(png);
            SetDirty();
            UpdateProfileImage();
        }
        finally
        {
            _profileImageBusy = false;
        }
    }

    private string GetProfileImageHintText()
    {
        return Loc.GetString(ProfileImageHintLoc,
            ("size", ImageLimits.Profile.MaxDimension),
            ("kilobytes", ImageLimits.Profile.MaxBytes / BytesPerKilobyte));
    }

    private async void ExportProfileImage()
    {
        var profile = Profile;
        if (_profileImageBusy || profile?.ProfileImage == null)
            return;

        _profileImageBusy = true;
        try
        {
            await _entManager.System<ImageTransferSystem>().ExportPngAsync(profile.ProfileImage);
        }
        finally
        {
            _profileImageBusy = false;
        }
    }

    private void RemoveProfileImage()
    {
        var profile = Profile;
        if (profile == null)
            return;

        Profile = profile.WithProfileImage(null);
        SetDirty();
        UpdateProfileImage();
    }

    private void UpdateProfileImage()
    {
        ProfileImageHint.Text = GetProfileImageHintText();

        var data = Profile?.ProfileImage;
        if (data == null
            || !_entManager.System<ImageTransferSystem>().TryCreatePreviewTexture(data, ProfileImagePreviewSize, out var texture))
        {
            ProfileImagePreview.Texture = null;
            return;
        }

        ProfileImagePreview.Texture = texture;
    }
}
