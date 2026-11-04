using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Ручной сканер имплантов. Быстрый скан называет только легальные импланты,
/// а глубокий скан лежачего или связанного человека может опознать и скрытые, но не наверняка.
/// </summary>
[RegisterComponent]
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
    /// Шанс, что глубокий скан опознает отдельный скрытый имплант.
    /// </summary>
    [DataField]
    public float IdentifyChance = 0.35f;

    /// <summary>
    /// Через сколько одного и того же человека можно снова глубоко сканировать.
    /// </summary>
    [DataField]
    public TimeSpan DeepScanCooldown = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Импланты, которые быстрый скан не может опознать и показывает как неизвестные сигнатуры.
    /// Наследники этих прототипов тоже считаются неопознаваемыми.
    /// </summary>
    [DataField]
    public List<EntProtoId> Concealed = new();

    /// <summary>
    /// Считаются ли клинки богомола скрытыми. Скрытые клинки опознаёт только глубокий скан, и то не наверняка.
    /// </summary>
    [DataField]
    public bool ConcealMantisBlades = true;
}
