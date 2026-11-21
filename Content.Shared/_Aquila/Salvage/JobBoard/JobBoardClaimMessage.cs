using Robust.Shared.Serialization;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Просьба клиента взять квоту на работу с доски работ.
/// </summary>
[Serializable, NetSerializable]
public sealed class JobBoardClaimMessage : BoundUserInterfaceMessage
{
    /// <summary>
    /// Идентификатор работы, на которую берётся квота.
    /// </summary>
    public string JobId;

    public JobBoardClaimMessage(string jobId)
    {
        JobId = jobId;
    }
}
