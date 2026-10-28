using Content.Shared._Aquila.TypingIndicator;
using Content.Shared.Chat.TypingIndicator;
using Robust.Shared.Prototypes;

namespace Content.Client.Chat.TypingIndicator;

public sealed partial class TypingIndicatorVisualizerSystem
{
    private bool TryApplyChannelIndicator(EntityUid uid, AppearanceComponent appearance, ref ProtoId<TypingIndicatorPrototype> indicator)
    {
        AppearanceSystem.TryGetData<TypingIndicatorState>(uid, TypingIndicatorVisuals.State, out var chatState, appearance);
        if (chatState == TypingIndicatorState.None
            && AppearanceSystem.TryGetData<ProtoId<TypingIndicatorPrototype>>(uid, TypingIndicatorChannelVisuals.Writing, out var writingIndicator, appearance))
        {
            indicator = writingIndicator;
            return true;
        }

        if (AppearanceSystem.TryGetData<ProtoId<TypingIndicatorPrototype>>(uid, TypingIndicatorChannelVisuals.Indicator, out var channelIndicator, appearance))
            indicator = channelIndicator;

        return false;
    }
}
