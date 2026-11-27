namespace Content.Shared._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Базовый тип системы лунного амулета. Лежит в Shared, чтобы атрибут <c>[Access]</c> на компонентах амулета
/// мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedMoonAmuletSystem : EntitySystem { }
