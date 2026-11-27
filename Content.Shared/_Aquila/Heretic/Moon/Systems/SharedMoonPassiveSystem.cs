namespace Content.Shared._Aquila.Heretic.Moon.Systems;

/// <summary>
/// Базовый тип системы пассивной силы пути Луны. Лежит в Shared, чтобы атрибут <c>[Access]</c> на компоненте
/// пассивной силы мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedMoonPassiveSystem : EntitySystem { }
