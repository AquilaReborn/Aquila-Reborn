namespace Content.Shared._Aquila.Emoting;

/// <summary>
/// Базовый тип системы принудительных эмоций. Лежит в Shared, чтобы атрибут <c>[Access]</c> на компоненте
/// статус-эффекта мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedForcedEmoteStatusEffectSystem : EntitySystem { }
