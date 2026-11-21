using Content.Shared.Cargo.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.Salvage.JobBoard;

/// <summary>
/// Дополнение состояния доски работ: список идентификаторов работ, квоты на которые уже взяты.
/// Список общий для всех, поэтому личных данных здесь нет.
/// </summary>
public sealed partial class SalvageJobBoardConsoleState
{
    /// <summary>
    /// Идентификаторы работ, квоты на которые уже взяты хоть кем-то из экипажа.
    /// </summary>
    public HashSet<string> TakenJobs = new();

    /// <summary>
    /// Создаёт состояние доски работ вместе со списком уже взятых квот.
    /// </summary>
    /// <param name="title">Название ранга станции.</param>
    /// <param name="progression">Прогресс до следующего ранга от 0 до 1.</param>
    /// <param name="availableJobs">Работы, которые сейчас видны на доске.</param>
    /// <param name="takenJobs">Идентификаторы работ, квоты на которые уже взяты.</param>
    public SalvageJobBoardConsoleState(string title, float progression, List<ProtoId<CargoBountyPrototype>> availableJobs, HashSet<string> takenJobs)
        : this(title, progression, availableJobs)
    {
        TakenJobs = takenJobs;
    }
}
