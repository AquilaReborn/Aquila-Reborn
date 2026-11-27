using Content.Shared.Heretic;

namespace Content.Shared._Shitcode.Heretic.Systems;

public abstract partial class SharedMansusGraspSystem
{
    /// <summary>
    /// Эффект хвата Мансуса для пути Луны. Переопределяется на сервере.
    /// </summary>
    /// <param name="performer">Кто применяет хват.</param>
    /// <param name="heretic">Компонент еретика кастующего.</param>
    /// <param name="target">Цель хвата.</param>
    protected virtual void ApplyMoonGrasp(EntityUid performer, HereticComponent heretic, EntityUid target) { }
}
