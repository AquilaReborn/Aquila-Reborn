using Content.Server._Aquila.Speech.EntitySystems;

namespace Content.Server._Aquila.Speech.Components;

/// <summary>
/// Vox accent: every Russian letter "к" in speech becomes "кик", and every "ч" becomes "чич".
/// </summary>
[RegisterComponent, Access(typeof(VoxAccentSystem))]
public sealed partial class VoxAccentComponent : Component
{
    /// <summary>
    /// Replacement for the lowercase Russian letter "к".
    /// </summary>
    [DataField]
    public string LowercaseReplacement = "кик";

    /// <summary>
    /// Replacement for the uppercase Russian letter "К".
    /// </summary>
    [DataField]
    public string UppercaseReplacement = "Кик";

    /// <summary>
    /// Replacement for the lowercase Russian letter "ч".
    /// </summary>
    [DataField]
    public string LowercaseChReplacement = "чич";

    /// <summary>
    /// Replacement for the uppercase Russian letter "Ч".
    /// </summary>
    [DataField]
    public string UppercaseChReplacement = "Чич";
}
