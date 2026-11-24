using Content.Server._Aquila.Heretic.Systems;

namespace Content.Server._Aquila.Heretic.Components;

/// <summary>
/// Запоминает, каким игрокам эта сущность уже сообщила о появлении рядом с ними,
/// чтобы повторный вход игрока в радиус снова поднимал событие, а стояние рядом его не повторяло.
/// </summary>
[RegisterComponent, UnsavedComponent, Access(typeof(ProximityAppearanceSystem)), AutoGenerateComponentPause]
public sealed partial class ProximityAppearanceComponent : Component
{
    /// <summary>
    /// Момент, с которого сущность снова проверяет окружение: перебор игроков идёт с троттлингом, а не каждый тик.
    /// </summary>
    [ViewVariables, AutoPausedField]
    public TimeSpan NextCheck;

    /// <summary>
    /// Игроки, которые уже находятся в радиусе появления и получили событие.
    /// </summary>
    [ViewVariables]
    public readonly HashSet<EntityUid> Notified = new();
}
