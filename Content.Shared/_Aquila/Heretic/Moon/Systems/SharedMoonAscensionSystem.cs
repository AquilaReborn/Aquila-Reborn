namespace Content.Shared._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Базовый тип системы вознесения пути Луны. Лежит в Shared, чтобы атрибут <c>[Access]</c> на компонентах
/// вознесения мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedMoonAscensionSystem : EntitySystem { }
