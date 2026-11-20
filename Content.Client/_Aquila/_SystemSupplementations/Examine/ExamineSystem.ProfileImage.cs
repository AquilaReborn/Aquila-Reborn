using System.Linq;
using Content.Client._Aquila.Images.Systems;
using Content.Client._Aquila.ProfileImage.Components;
using Content.Client._Aquila.ProfileImage.UI;
using Content.Shared._Aquila.Images.Helpers;
using Content.Shared._Aquila.Images.Types;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Examine;

public sealed partial class ExamineSystem
{
    [Dependency] private readonly ImageTransferSystem _profileImageTransfer = default!;

    private const string ExamineVBoxName = "ExaminePopupVbox";
    private const string ProfileImageControlName = "ExamineProfileImage";
    private const int ProfileImagePreviewSide = 128;

    /// <summary>
    /// Shows the profile image preview in the currently opened details examine window.
    /// </summary>
    /// <param name="target">Examined entity the opened window must belong to.</param>
    /// <param name="png">PNG data to display, or <see langword="null"/> to remove the preview from the window.</param>
    public void ShowProfileImage(EntityUid target, byte[]? png)
    {
        var popup = _examineTooltipOpen;
        if (popup == null || !popup.Visible || _examinedEntity != target)
            return;

        if (FindExamineVBox(popup) is not { } vBox)
            return;

        var existing = vBox.Children.FirstOrDefault(child => child.Name == ProfileImageControlName);
        if (existing != null)
            existing.Orphan();

        if (!_userInterfaceManager.GetUIController<ProfileImageUIController>().IsDetailsMode() || png == null)
            return;

        if (!PngHelper.TryGetPngDimensions(png, out var width, out var height)
            || !ImageLimits.Profile.AllowsDimension(width, height))
            return;

        if (!_profileImageTransfer.TryCreatePreviewTexture(png, ProfileImagePreviewSide, out var preview)
            || preview == null)
            return;

        var button = new TextureButton
        {
            Name = ProfileImageControlName,
            TextureNormal = preview,
            Margin = new Thickness(6, 0, 6, 6),
            SetSize = preview.Size,
        };
        button.OnPressed += _ => OpenProfileImageViewerAquila(png);

        vBox.AddChild(button);
    }

    private static BoxContainer? FindExamineVBox(Control control)
    {
        foreach (var child in control.Children)
        {
            if (child is BoxContainer box && box.Name == ExamineVBoxName)
                return box;

            if (FindExamineVBox(child) is { } found)
                return found;
        }

        return null;
    }

    private void ApplyProfileImageAquila(EntityUid target)
    {
        if (TryComp<ClientProfileImageComponent>(target, out var cached))
            ShowProfileImage(target, cached.Png);
    }

    private void OpenProfileImageViewerAquila(byte[] png)
    {
        if (!_profileImageTransfer.TryCreateTexture(png, out var texture) || texture == null)
            return;

        _examineTooltipOpen?.Close();
        _userInterfaceManager.GetUIController<ProfileImageUIController>().OpenViewer(texture);
    }

    private void ResetDetailsModeAquila()
    {
        _userInterfaceManager.GetUIController<ProfileImageUIController>().ResetDetailsMode();
    }
}
