namespace Content.Server._Aquila.Heretic.Moon.Components;

/// <summary>
/// Правило, через которое вознёсшийся еретик пути Луны обращает экипаж в лунатиков.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class MoonLunaticRuleComponent : Component;
