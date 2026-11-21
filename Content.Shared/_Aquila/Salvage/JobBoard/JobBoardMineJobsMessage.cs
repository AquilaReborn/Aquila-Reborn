using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Личный список квот игрока: какие работы взял именно он. Отправляется только ему,
/// в общее состояние доски эти данные не попадают.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobBoardMineJobsMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Идентификаторы работ, квоты на которые взял получатель сообщения.
    /// </summary>
    public HashSet<string> MineJobs;

    public JobBoardMineJobsMessage(HashSet<string> mineJobs)
    {
        MineJobs = mineJobs;
    }
}
