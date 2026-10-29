namespace Content.Shared.Weapons.Reflect;

public sealed partial class ReflectSystem
{
    /// <summary>
    /// Возвращает шанс отражения снаряда указанного типа.
    /// </summary>
    public float GetReflectProb(Entity<ReflectComponent?> reflector, ReflectType type)
    {
        if (!Resolve(reflector, ref reflector.Comp, false))
            return 0f;

        if ((reflector.Comp.Reflects & type) != 0x0)
            return reflector.Comp.ReflectProb;

        return (type & (ReflectType.Energy | ReflectType.NonEnergy)) != 0x0 ? reflector.Comp.OtherTypeReflectProb : 0f;
    }
}
