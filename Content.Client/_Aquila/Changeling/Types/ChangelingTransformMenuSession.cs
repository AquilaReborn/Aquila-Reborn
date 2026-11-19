using Content.Client.UserInterface.Controls;

namespace Content.Client._Aquila.Changeling.Types;

/// <summary>
///     Controls and local preview entities owned by one changeling menu instance.
/// </summary>
public sealed class ChangelingTransformMenuSession(SimpleRadialMenu menu, Action<int> selectForm)
{
    /// <summary>
    ///     Radial menu for this interface instance.
    /// </summary>
    public readonly SimpleRadialMenu Menu = menu;

    /// <summary>
    ///     Local dolls displayed by this menu.
    /// </summary>
    public readonly List<EntityUid> Previews = new();

    /// <summary>
    ///     Sends the selected form index through the owning bound interface.
    /// </summary>
    public readonly Action<int> SelectForm = selectForm;
}
