using Robust.Shared.CPUJob.JobQueues;

namespace Content.Client.Replay;

public sealed partial class ContentLoadReplayJob
{
    private bool _loadCancelled;

    internal void CancelReplayLoad()
    {
        _loadCancelled = true;
        if (Status == JobStatus.Paused)
            Run();
    }

    private void ThrowIfReplayLoadCancelled()
    {
        if (_loadCancelled)
            throw new OperationCanceledException();
    }
}
