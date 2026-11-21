using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Дополнение консоли доски работ: кулдаун на взятие личной квоты, чтобы кнопку нельзя было заспамить.
/// </summary>
public sealed partial class SalvageJobBoardConsoleComponent
{
    /// <summary>
    /// Время, после которого консоль снова примет взятие квоты.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextClaimTime = TimeSpan.Zero;

    /// <summary>
    /// Задержка между взятиями квот.
    /// </summary>
    [DataField]
    public TimeSpan ClaimDelay = TimeSpan.FromSeconds(1);
}
