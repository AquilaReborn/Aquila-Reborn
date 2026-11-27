namespace Content.Shared._Shitcode.Heretic.Systems.Abilities;

public abstract partial class SharedHereticAbilitySystem
{
    /// <summary>
    /// Регистрирует обработчики способностей пути Луны. Переопределяется на сервере.
    /// </summary>
    protected virtual void SubscribeMoon() { }
}
