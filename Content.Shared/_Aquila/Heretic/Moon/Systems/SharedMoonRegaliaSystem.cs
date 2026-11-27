namespace Content.Shared._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Базовый тип системы пышного облачения пути Луны. Лежит в Shared, чтобы атрибут <c>[Access]</c> на компонентах
/// облачения мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedMoonRegaliaSystem : EntitySystem { }
