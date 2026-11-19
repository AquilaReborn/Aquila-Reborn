using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Goobstation.Shared._Aquila.Changeling.Types;

/// <summary>
///     Visual data used to create a client-only preview of a stored form.
/// </summary>
[Serializable, NetSerializable]
public sealed class ChangelingTransformAppearance(HumanoidAppearanceComponent appearance)
{
    /// <summary>
    ///     Species of the stored form.
    /// </summary>
    public readonly ProtoId<SpeciesPrototype> Species = appearance.Species;

    /// <summary>
    ///     Sex of the stored form.
    /// </summary>
    public readonly Sex Sex = appearance.Sex;

    /// <summary>
    ///     Gender of the stored form.
    /// </summary>
    public readonly Gender Gender = appearance.Gender;

    /// <summary>
    ///     Age of the stored form.
    /// </summary>
    public readonly int Age = appearance.Age;

    /// <summary>
    ///     Height of the stored form.
    /// </summary>
    public readonly float Height = appearance.Height;

    /// <summary>
    ///     Width of the stored form.
    /// </summary>
    public readonly float Width = appearance.Width;

    /// <summary>
    ///     Skin color of the stored form.
    /// </summary>
    public readonly Color SkinColor = appearance.SkinColor;

    /// <summary>
    ///     Eye color of the stored form.
    /// </summary>
    public readonly Color EyeColor = appearance.EyeColor;

    /// <summary>
    ///     Markings of the stored form.
    /// </summary>
    public readonly MarkingSet MarkingSet = new(appearance.MarkingSet);

    /// <summary>
    ///     Custom body layers of the stored form.
    /// </summary>
    public readonly Dictionary<HumanoidVisualLayers, CustomBaseLayerInfo> CustomBaseLayers = new(appearance.CustomBaseLayers);
}
