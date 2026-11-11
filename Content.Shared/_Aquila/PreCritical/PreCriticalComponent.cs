namespace Content.Shared._Aquila.PreCritical;

/// <summary>
/// Состояние моба между живым и критическим: он ещё жив, но уже не может стоять.
/// Пока урон держится в полосе предкрита, моб лежит, ползёт и работает руками;
/// урон выше критического порога отправляет его в крит, а лечение возвращает в живое состояние.
/// </summary>
[RegisterComponent, Access(typeof(PreCriticalSystem))]
public sealed partial class PreCriticalComponent : Component
{
    /// <summary>
    /// Сколько времени моб остаётся лежачим. Значение с большим запасом, чтобы моб
    /// ползал до лечения или следующего урона, а не вставал сам по таймеру.
    /// </summary>
    [DataField]
    public TimeSpan HoldTime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Множитель скорости движения поверх скорости ползания, пока моб в предкритическом состоянии.
    /// </summary>
    [DataField]
    public float CrawlSpeedMultiplier = 0.5f;
}
