using Content.Shared._EinsteinEngines.Language.Components.Translators;

namespace Content.Shared._Aquila.Language.Components;

/// <summary>
/// Языковой чип. Вставляется в языковой имплант и даёт его владельцу знание языков, пока остаётся внутри.
/// </summary>
[RegisterComponent]
public sealed partial class TranslatorChipComponent : BaseTranslatorComponent;
