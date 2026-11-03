using Robust.Shared.Audio;

namespace Content.Shared._Aquila.Language.Components;

/// <summary>
/// Языковой имплант, в который вставляются языковые чипы.
/// </summary>
[RegisterComponent, Access(typeof(SharedTranslatorChipSystem))]
public sealed partial class TranslatorChipHolderComponent : Component
{
    public const string ContainerId = "translator_chips";

    /// <summary>
    /// Сколько чипов помещается в имплант.
    /// </summary>
    [DataField]
    public int MaxChips = 4;

    /// <summary>
    /// Сколько времени занимает вставка чипа.
    /// </summary>
    [DataField]
    public TimeSpan InsertDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Звук вставки и извлечения чипа.
    /// </summary>
    [DataField]
    public SoundSpecifier? InsertSound = new SoundPathSpecifier("/Audio/Machines/id_insert.ogg");
}
