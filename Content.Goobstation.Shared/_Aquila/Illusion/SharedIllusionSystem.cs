using Content.Goobstation.Shared.Illusion;

namespace Content.Goobstation.Shared._Aquila.Illusion;

/// <summary>
/// Базовый тип систем иллюзий. Лежит в Shared, чтобы атрибут <c>[Access]</c> на полях
/// <see cref="IllusionComponent"/> мог разрешить запись серверной системе: она наследуется от этого типа.
/// </summary>
public abstract class SharedIllusionSystem : EntitySystem { }
