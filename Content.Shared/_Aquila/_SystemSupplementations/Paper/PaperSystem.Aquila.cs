using static Content.Shared.Paper.PaperComponent;

namespace Content.Shared.Paper;

public sealed partial class PaperSystem
{
    private void StopTypingForReaders(EntityUid paper)
    {
        foreach (var actor in _uiSystem.GetActors(paper, PaperUiKey.Key))
        {
            _typingIndicatorChannel.SetWriting(actor, false);
        }
    }
}
