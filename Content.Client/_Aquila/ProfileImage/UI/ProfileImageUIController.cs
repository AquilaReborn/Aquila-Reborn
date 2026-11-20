using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._Aquila.ProfileImage.UI;

/// <summary>
/// Keeps the profile image UI state and opens the profile image viewer window.
/// </summary>
public sealed class ProfileImageUIController : UIController
{
    private bool _detailsMode;
    private ProfileImageViewerWindow? _viewer;

    /// <summary>
    /// Allows the profile image preview to be shown in the examine window that is currently open.
    /// </summary>
    public void SetDetailsMode()
    {
        _detailsMode = true;
    }

    /// <summary>
    /// Forbids the profile image preview in examine windows.
    /// </summary>
    public void ResetDetailsMode()
    {
        _detailsMode = false;
    }

    /// <summary>
    /// Checks whether the profile image preview is allowed in the examine window that is currently open.
    /// </summary>
    /// <returns><see langword="true"/> if the profile image preview is allowed.</returns>
    public bool IsDetailsMode()
    {
        return _detailsMode;
    }

    /// <summary>
    /// Shows the given texture in the profile image viewer, opening the window if it is closed.
    /// </summary>
    /// <param name="texture">Texture to display.</param>
    public void OpenViewer(Texture texture)
    {
        var viewer = _viewer ?? new ProfileImageViewerWindow();
        _viewer = viewer;
        viewer.SetImage(texture);
        viewer.OpenCentered();
    }
}
