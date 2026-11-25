using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Ручной сканер имплантов. Быстрый скан называет только легальные импланты,
/// а глубокий скан лежачего или связанного человека опознаёт и скрытые.
/// </summary>
[RegisterComponent, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class CyberwareScannerComponent : Component
{
    /// <summary>
    /// Сколько длится быстрое сканирование.
    /// </summary>
    [DataField]
    public TimeSpan ScanTime = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Сколько длится глубокое сканирование.
    /// </summary>
    [DataField]
    public TimeSpan DeepScanTime = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Через сколько одного и того же человека можно снова глубоко сканировать.
    /// </summary>
    [DataField]
    public TimeSpan DeepScanCooldown = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Импланты, которые быстрый скан не опознаёт.
    /// Наследники этих прототипов тоже считаются неопознаваемыми.
    /// </summary>
    [DataField]
    public List<EntProtoId> Concealed = new();

    /// <summary>
    /// Считаются ли клинки богомола скрытыми. Скрытые клинки опознаёт только глубокий скан.
    /// </summary>
    [DataField]
    public bool ConcealMantisBlades = true;
}
