using Content.Server._Aquila.Speech.EntitySystems;

namespace Content.Server._Aquila.Speech.Components;

/// <summary>
/// Акцент воксов: каждая русская буква "к" в речи превращается в "кик".
/// </summary>
[RegisterComponent, Access(typeof(VoxAccentSystem))]
public sealed partial class VoxAccentComponent : Component
{
    /// <summary>
    /// Замена для строчной русской буквы "к".
    /// </summary>
    [DataField]
    public string LowercaseReplacement = "кик";

    /// <summary>
    /// Замена для заглавной русской буквы "К".
    /// </summary>
    [DataField]
    public string UppercaseReplacement = "Кик";
}
