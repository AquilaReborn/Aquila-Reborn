using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared._Aquila.Changeling.Types;

/// <summary>
///     A single selectable form shown in the changeling form selection menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformEntry(int index, string name, bool current, bool selected,
    ChangelingTransformAppearance appearance)
{
    /// <summary>
    ///     Zero-based index of the form. Zero is always the original form.
    /// </summary>
    public readonly int Index = index;

    /// <summary>
    ///     Display name of the form.
    /// </summary>
    public readonly string Name = name;

    /// <summary>
    ///     Whether the changeling is currently in this form.
    /// </summary>
    public readonly bool Current = current;

    /// <summary>
    ///     Whether this form is currently selected for transformation.
    /// </summary>
    public readonly bool Selected = selected;

    /// <summary>
    ///     Appearance of the local preview doll.
    /// </summary>
    public readonly ChangelingTransformAppearance Appearance = appearance;
}

/// <summary>
///     State of the changeling form selection menu.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformState(List<ChangelingTransformEntry> forms) : BoundUserInterfaceState
{
    /// <summary>
    ///     All forms available to the changeling.
    /// </summary>
    public readonly List<ChangelingTransformEntry> Forms = forms;
}

/// <summary>
///     Requests selecting a form by its index in <see cref="ChangelingTransformState.Forms"/>.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformIdentitySelectMessage(int index) : BoundUserInterfaceMessage
{
    /// <summary>
    ///     Zero-based index of the selected form.
    /// </summary>
    public readonly int Index = index;
}
