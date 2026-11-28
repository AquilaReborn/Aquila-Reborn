using Robust.Shared.Audio;

namespace Content.Server._Aquila.Heretic.Moon.Components;

/// <summary>
/// Правило, через которое вознёсшийся еретик пути Луны обращает экипаж в лунатиков.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class MoonLunaticRuleComponent : Component
{
    /// <summary>
    /// Музыка, которая играет обращённому в лунатика вместе с брифингом.
    /// </summary>
    [DataField]
    public SoundSpecifier BriefingSound = new SoundPathSpecifier("/Audio/_Aquila/Heretic/Moon/moon_parade_short.ogg");
}
