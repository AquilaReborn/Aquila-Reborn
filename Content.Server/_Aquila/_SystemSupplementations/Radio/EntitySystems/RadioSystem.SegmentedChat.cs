using System.Text;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Content.Shared.StatusIcon;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    private IReadOnlyList<ChatMessageChunk>? _aquilaSegments;

    /// <summary>
    /// Запоминает куски размеченного сообщения для следующей радиопередачи. null сбрасывает их.
    /// </summary>
    public void SetAquilaSegments(IReadOnlyList<ChatMessageChunk>? chunks)
    {
        _aquilaSegments = chunks;
    }

    private IReadOnlyList<ChatMessageChunk>? TakeAquilaSegments(string message)
    {
        var chunks = _aquilaSegments;
        _aquilaSegments = null;

        if (chunks is not { Count: > 0 })
            return null;

        var builder = new StringBuilder();

        foreach (var chunk in chunks)
            builder.Append(chunk.Text);

        return builder.ToString() == message ? chunks : null;
    }

    private bool TryAquilaSendSegmentedRadio(
        EntityUid receiver,
        EntityUid messageSource,
        RadioChannelPrototype channel,
        string name,
        LanguagePrototype language,
        ProtoId<JobIconPrototype>? jobIcon,
        string? jobName,
        int? frequency,
        EntityUid radioSource,
        IReadOnlyList<ChatMessageChunk>? chunks)
    {
        if (chunks is not { Count: > 0 })
            return false;

        var listener = receiver;
        var parent = Transform(receiver).ParentUid;

        if (parent.IsValid() && HasComp<ActorComponent>(parent))
            listener = parent;

        var body = _chat.BuildAquilaRadioBody(chunks, messageSource, listener, language, out var plain);
        var wrapped = WrapRadioMessage(messageSource, channel, name, body, language, jobIcon, jobName, frequency);

        var composed = new ChatMessage(ChatChannel.Radio, plain, wrapped, GetNetEntity(messageSource), null);
        var ev = new RadioReceiveEvent(messageSource, channel, composed, composed, language, radioSource);
        RaiseLocalEvent(receiver, ref ev);
        return true;
    }
}
