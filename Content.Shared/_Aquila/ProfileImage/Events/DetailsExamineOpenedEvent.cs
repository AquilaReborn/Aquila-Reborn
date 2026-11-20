namespace Content.Shared._Aquila.ProfileImage.Events;

/// <summary>
/// Raised locally when the details examine window of an entity is opened.
/// </summary>
public sealed class DetailsExamineOpenedEvent : EntityEventArgs
{
    /// <summary>
    /// Entity whose details are shown in the window.
    /// </summary>
    public readonly EntityUid Target;

    /// <summary>
    /// Creates a new event for the opened details examine window.
    /// </summary>
    /// <param name="target">Entity whose details are shown in the window.</param>
    public DetailsExamineOpenedEvent(EntityUid target)
    {
        Target = target;
    }
}
