using System.Globalization;
using System.Text;
using Content.Goobstation.Common.Chat;
using Content.Server._Aquila.Chat;
using Content.Shared._EinsteinEngines.Language;
using Content.Shared._Goobstation.Wizard.Chuuni;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Database;
using Content.Shared.IdentityManagement;
using Content.Shared.Players.RateLimiting;
using Content.Shared.Radio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server.Chat.Systems;

public sealed partial class ChatSystem
{
    [Dependency] private readonly INetManager _netManager = default!;

    private const int MaxAquilaSegments = 32;

    private List<ChatMessageSegment>? _aquilaRawSegments;
    private List<ChatMessageChunk>? _aquilaSegments;
    private string? _aquilaOriginalText;

    private void InitializeAquila()
    {
        _netManager.RegisterNetMessage<MsgSegmentedChatMessage>(OnAquilaSegmentedChat);
    }

    private void OnAquilaSegmentedChat(MsgSegmentedChatMessage msg)
    {
        if (!_playerManager.TryGetSessionByChannel(msg.MsgChannel, out var session))
            return;

        if (session.AttachedEntity is not { Valid: true } uid)
            return;

        _aquilaRawSegments = msg.Segments;
        _aquilaOriginalText = msg.Text;

        try
        {
            if (!string.IsNullOrEmpty(msg.RadioChannel))
            {
                var radioChannelId = msg.RadioChannel;

                if (_prototypeManager.TryIndex<RadioChannelPrototype>(radioChannelId, out var radioChannel))
                    TrySendAquilaSegmentedRadio(uid, msg.Text, msg.Segments, radioChannel, session);

                return;
            }

            TrySendInGameICMessage(
                uid,
                msg.Text,
                msg.Whisper ? InGameICChatType.Whisper : InGameICChatType.Speak,
                ChatTransmitRange.Normal,
                player: session,
                checkRadioPrefix: false);
        }
        finally
        {
            _aquilaRawSegments = null;
            _aquilaSegments = null;
            _aquilaOriginalText = null;
        }
    }

    private string SanitizeAquila(EntityUid source, string message, out string? emoteStr, bool capitalize, bool punctuate, bool capitalizeTheWordI)
    {
        if (_aquilaRawSegments is not { Count: > 0 })
            return SanitizeInGameICMessage(source, message, out emoteStr, capitalize, punctuate, capitalizeTheWordI);

        emoteStr = null;
        return SanitizeAquilaSegmented(source, message, capitalize, punctuate, capitalizeTheWordI);
    }

    private bool TrySendAquilaSegmentedMessage(
        EntityUid source,
        string message,
        string originalMessage,
        InGameICChatType type,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language,
        bool hideLog,
        bool ignoreActionBlocker,
        Color? colorOverride)
    {
        var segments = _aquilaSegments;
        _aquilaSegments = null;

        if (segments is not { Count: > 0 })
            return false;

        if (type != InGameICChatType.Speak && type != InGameICChatType.Whisper)
            return false;

        SendAquilaSegmentedMessage(source, message, originalMessage, segments, type, range, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride);
        return true;
    }

    private void TrySendAquilaSegmentedRadio(EntityUid source, string message, List<ChatMessageSegment> segments, RadioChannelPrototype channel, ICommonSession player)
    {
        if (_chatManager.HandleRateLimit(player) != RateLimitStatus.Allowed)
            return;

        if (!CanSendInGame(message, player: player))
            return;

        var ignoreActionBlocker = CheckIgnoreSpeechBlocker(source, false);
        var language = _language.GetLanguage(source);

        var colorEv = new GetMessageColorOverrideEvent();
        RaiseLocalEvent(source, colorEv);
        var colorOverride = colorEv.Color;

        _aquilaRawSegments = segments;

        var shouldCapitalizeTheWordI = (!CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Parent.Name == "en")
            || (CultureInfo.CurrentCulture.IsNeutralCulture && CultureInfo.CurrentCulture.Name == "en");

        var sanitized = SanitizeAquilaSegmented(source, message, true, _configurationManager.GetCVar(CCVars.ChatPunctuation), shouldCapitalizeTheWordI);

        var chunks = _aquilaSegments;
        _aquilaSegments = null;

        if (string.IsNullOrEmpty(sanitized) || chunks is not { Count: > 0 })
            return;

        SendAquilaSegmentedWhisper(source, sanitized, message, chunks, ChatTransmitRange.Normal, null, language, false, ignoreActionBlocker, colorOverride, channel);
    }

    /// <summary>
    /// Собирает итоговый текст сообщения из участков, присланных клиентом, и заполняет список кусков для отправки.
    /// Пробелы по краям обрезаются вместе со сдвигом границ участков, пересечения обрезаются по концу более раннего
    /// участка, а всё сверх <see cref="MaxAquilaSegments"/> участков отбрасывается.
    /// Эмодзи-шорткаты для размеченных сообщений не обрабатываются, акценты речи применяются к каждому участку отдельно.
    /// </summary>
    private string SanitizeAquilaSegmented(EntityUid source, string message, bool capitalize, bool punctuate, bool capitalizeTheWordI)
    {
        var raw = _aquilaRawSegments;
        _aquilaRawSegments = null;
        _aquilaSegments = null;

        var defaultLanguage = _language.GetLanguage(source);
        var trimmed = message.Trim();

        if (trimmed.Length == 0)
            return string.Empty;

        var leading = message.Length - message.TrimStart().Length;
        var normalized = new List<(int Start, int Length, ProtoId<LanguagePrototype> Language)>();

        if (raw != null)
        {
            foreach (var segment in raw)
            {
                if (segment.Start < 0 || segment.Length <= 0 || segment.Start + segment.Length > message.Length)
                    continue;

                if (!_prototypeManager.TryIndex<LanguagePrototype>(segment.Language, out var language))
                    continue;

                if (!_language.CanSpeak(source, language.ID))
                    continue;

                var start = Math.Max(segment.Start - leading, 0);
                var end = Math.Min(segment.Start + segment.Length - leading, trimmed.Length);

                if (end <= start)
                    continue;

                normalized.Add((start, end - start, language.ID));
            }
        }

        normalized.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : b.Length.CompareTo(a.Length));

        var accepted = new List<(int Start, int Length, ProtoId<LanguagePrototype> Language)>();
        var occupied = 0;

        foreach (var segment in normalized)
        {
            if (accepted.Count >= MaxAquilaSegments)
                break;

            var start = Math.Max(segment.Start, occupied);
            var end = segment.Start + segment.Length;

            if (end <= start)
                continue;

            accepted.Add((start, end - start, segment.Language));
            occupied = end;
        }

        var pieces = new List<ChatMessageChunk>();
        var cursor = 0;

        foreach (var segment in accepted)
        {
            if (cursor < segment.Start)
                pieces.Add(new ChatMessageChunk { Text = trimmed[cursor..segment.Start], Language = defaultLanguage.ID });

            pieces.Add(new ChatMessageChunk { Text = trimmed[segment.Start..(segment.Start + segment.Length)], Language = segment.Language });
            cursor = segment.Start + segment.Length;
        }

        if (cursor < trimmed.Length)
            pieces.Add(new ChatMessageChunk { Text = trimmed[cursor..], Language = defaultLanguage.ID });

        if (pieces.Count == 0)
            pieces.Add(new ChatMessageChunk { Text = trimmed, Language = defaultLanguage.ID });

        var result = new List<ChatMessageChunk>(pieces.Count);
        var builder = new StringBuilder();
        var capitalized = false;

        for (var i = 0; i < pieces.Count; i++)
        {
            var piece = FormattedMessage.EscapeText(FormattedMessage.RemoveMarkupPermissive(SanitizeMessageReplaceWords(pieces[i].Text)));

            if (capitalize && !capitalized && piece.Length > 0)
            {
                piece = SanitizeMessageCapital(piece);
                capitalized = true;
            }

            if (capitalizeTheWordI)
                piece = SanitizeMessageCapitalizeTheWordI(piece, "i");

            if (punctuate && i == pieces.Count - 1)
                piece = SanitizeMessagePeriod(piece);

            if (piece.Length > 0 && _prototypeManager.TryIndex<LanguagePrototype>(pieces[i].Language, out var languageProto))
                piece = TransformSpeech(source, piece, languageProto);

            result.Add(new ChatMessageChunk { Text = piece, Language = pieces[i].Language });
            builder.Append(piece);
        }

        _aquilaSegments = result;
        return builder.ToString();
    }

    private void SendAquilaSegmentedMessage(
        EntityUid source,
        string message,
        string originalMessage,
        List<ChatMessageChunk> segments,
        InGameICChatType type,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language,
        bool hideLog,
        bool ignoreActionBlocker,
        Color? colorOverride)
    {
        switch (type)
        {
            case InGameICChatType.Speak:
                SendAquilaSegmentedSpeak(source, message, originalMessage, segments, range, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride);
                break;
            case InGameICChatType.Whisper:
                SendAquilaSegmentedWhisper(source, message, originalMessage, segments, range, nameOverride, language, hideLog, ignoreActionBlocker, colorOverride);
                break;
        }
    }

    private void SendAquilaSegmentedSpeak(
        EntityUid source,
        string message,
        string originalMessage,
        List<ChatMessageChunk> segments,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language,
        bool hideLog,
        bool ignoreActionBlocker,
        Color? colorOverride)
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        if (message.Length == 0)
            return;

        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }

        name = FormattedMessage.EscapeText(name);

        var typeLOS = SpeakRespectsLOS || language.SpeechOverride.RequireLOS;
        if (!language.SpeechOverride.RequireSpeech && language.SpeechOverride.RequireLOS)
        {
            var ent = Identity.Entity(source, EntityManager);
            name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));
        }

        foreach (var (session, data) in GetRecipients(source, VoiceRange))
        {
            var entRange = MessageRangeCheck(session, data, range);
            if (entRange == MessageRangeCheckResult.Disallowed)
                continue;

            var entHideChat = entRange == MessageRangeCheckResult.HideChat;

            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            if (typeLOS && !data.Observer && !data.InLOS)
                continue;

            var vacuumHearing = _vacuumChat.GetHearing(source, listener);
            if (vacuumHearing == VacuumHearing.None)
                continue;

            if (TrySendUnclearMessage(ChatChannel.Local, listener, source, entHideChat, session.Channel))
                continue;

            var ev = new ChatMessageOverrideInRange(language.SpeechOverride.RequireSpeech, language.SpeechOverride.RequireSight);
            RaiseLocalEvent(listener, ref ev);
            if (ev.Cancelled)
                continue;

            var (body, plainMessage) = BuildAquilaSegmentedBody(segments, source, listener, language, false);
            var wrappedMessage = WrapPublicMessage(source, name, body, language, colorOverride);

            _chatManager.ChatMessageToOne(ChatChannel.Local, plainMessage, _vacuumChat.WrapMessage(vacuumHearing, wrappedMessage), source, entHideChat, session.Channel);
        }

        var replayWrap = WrapPublicMessage(source, name, message, language, colorOverride);
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Local, message, replayWrap, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        var evSpoke = new EntitySpokeEvent(source, message, null, false, language);
        RaiseLocalEvent(source, evSpoke, true);

        if (!HasComp<ActorComponent>(source) || hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Say from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Say from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private void SendAquilaSegmentedWhisper(
        EntityUid source,
        string message,
        string originalMessage,
        List<ChatMessageChunk> segments,
        ChatTransmitRange range,
        string? nameOverride,
        LanguagePrototype language,
        bool hideLog,
        bool ignoreActionBlocker,
        Color? colorOverride,
        RadioChannelPrototype? radioChannel = null)
    {
        if (!_actionBlocker.CanSpeak(source) && !ignoreActionBlocker)
            return;

        if (message.Length == 0)
            return;

        var nameIdentity = FormattedMessage.EscapeText(nameOverride ?? Identity.Name(source, EntityManager));

        string name;
        if (nameOverride != null)
        {
            name = nameOverride;
        }
        else
        {
            var nameEv = new TransformSpeakerNameEvent(source, Name(source));
            RaiseLocalEvent(source, nameEv);
            name = nameEv.VoiceName;
        }

        name = FormattedMessage.EscapeText(name);

        if (!language.SpeechOverride.RequireSpeech && language.SpeechOverride.RequireLOS)
        {
            var ent = Identity.Entity(source, EntityManager);
            name = FormattedMessage.EscapeText(nameOverride ?? Name(ent));
        }

        foreach (var (session, data) in GetRecipients(source, WhisperMuffledRange))
        {
            if (session.AttachedEntity is not { Valid: true } listener)
                continue;

            var vacuumHearing = _vacuumChat.GetHearing(source, listener);
            if (vacuumHearing == VacuumHearing.None)
                continue;

            if (MessageRangeCheck(session, data, range) != MessageRangeCheckResult.Full)
                continue;

            if (TrySendUnclearMessage(ChatChannel.Whisper, listener, source, false, session.Channel))
                continue;

            var evSight = new ChatMessageOverrideInRange(language.SpeechOverride.RequireSpeech, language.SpeechOverride.RequireSight);
            RaiseLocalEvent(listener, ref evSight);
            if (evSight.Cancelled)
                continue;

            LocId wrapId;
            string wrapName;
            var obfuscateReadability = false;

            if (!language.SpeechOverride.RequireLOS && data.Range <= WhisperClearRange
                || _examineSystem.InRangeUnOccluded(source, listener, WhisperClearRange)
                || data.Observer)
            {
                wrapId = "chat-manager-entity-whisper-wrap-message";
                wrapName = name;
            }
            else if (_examineSystem.InRangeUnOccluded(source, listener, WhisperMuffledRange))
            {
                obfuscateReadability = true;
                wrapId = "chat-manager-entity-whisper-wrap-message";
                wrapName = nameIdentity;
            }
            else
            {
                if (language.SpeechOverride.RequireLOS)
                    continue;

                obfuscateReadability = true;
                wrapId = "chat-manager-entity-whisper-unknown-wrap-message";
                wrapName = string.Empty;
            }

            var (body, plainMessage) = BuildAquilaSegmentedBody(segments, source, listener, language, obfuscateReadability);
            var wrappedMessage = WrapWhisperMessage(source, wrapId, wrapName, body, language, colorOverride);

            _chatManager.ChatMessageToOne(ChatChannel.Whisper, plainMessage, _vacuumChat.WrapMessage(vacuumHearing, wrappedMessage), source, false, session.Channel);
        }

        var replayWrap = WrapWhisperMessage(source, "chat-manager-entity-whisper-wrap-message", name, message, language, colorOverride);
        _replay.RecordServerMessage(new ChatMessage(ChatChannel.Whisper, message, replayWrap, GetNetEntity(source), null, MessageRangeHideChatForReplay(range)));

        var evSpoke = new EntitySpokeEvent(source, message, radioChannel, true, language);
        evSpoke.Chunks = segments;
        RaiseLocalEvent(source, evSpoke, true);

        if (hideLog)
            return;

        if (originalMessage == message)
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source} as {name}: {originalMessage}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low, $"Whisper from {source}: {originalMessage}.");
        }
        else
        {
            if (name != Name(source))
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source} as {name}, original: {originalMessage}, transformed: {message}.");
            else
                _adminLogger.Add(LogType.Chat, LogImpact.Low,
                    $"Whisper from {source}, original: {originalMessage}, transformed: {message}.");
        }
    }

    private (string Body, string Plain) BuildAquilaSegmentedBody(
        IReadOnlyList<ChatMessageChunk> chunks,
        EntityUid source,
        EntityUid listener,
        LanguagePrototype defaultLanguage,
        bool obfuscateReadability)
    {
        var body = BuildAquilaBody(chunks, source, listener, defaultLanguage, obfuscateReadability, out var plain);
        return (body, plain);
    }

    /// <summary>
    /// Собирает тело радиосообщения из кусков с языками: понятные куски идут как есть, непонятные обфусцируются
    /// по правилам своего языка, не-дефолтные куски получают цвет своего языка. Хвостовая пунктуация последнего
    /// цветного куска остаётся после закрывающего тега, чтобы работали суффиксные глаголы речи.
    /// В <paramref name="plain"/> возвращается тот же текст без разметки.
    /// </summary>
    public string BuildAquilaRadioBody(IReadOnlyList<ChatMessageChunk> chunks, EntityUid source, EntityUid listener, LanguagePrototype defaultLanguage, out string plain)
    {
        return BuildAquilaBody(chunks, source, listener, defaultLanguage, false, out plain);
    }

    private string BuildAquilaBody(
        IReadOnlyList<ChatMessageChunk> chunks,
        EntityUid source,
        EntityUid listener,
        LanguagePrototype defaultLanguage,
        bool obfuscateReadability,
        out string plain)
    {
        var body = new StringBuilder();
        var plainBuilder = new StringBuilder();

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var isDefault = chunk.Language == defaultLanguage.ID;
            LanguagePrototype? language = null;

            if (isDefault)
                language = defaultLanguage;
            else
                _prototypeManager.TryIndex<LanguagePrototype>(chunk.Language, out language);

            var piece = chunk.Text;

            if (listener != source && (language == null || !_language.CanUnderstand(listener, chunk.Language)))
            {
                if (language != null)
                    piece = _language.ObfuscateSpeech(piece, language);
            }

            if (obfuscateReadability)
                piece = ObfuscateMessageReadability(piece);

            plainBuilder.Append(piece);

            Color? speechColor = isDefault ? null : language?.SpeechOverride.Color;

            if (speechColor == null)
            {
                body.Append(piece);
                continue;
            }

            var core = piece;
            var tail = string.Empty;

            if (i == chunks.Count - 1)
            {
                var end = piece.Length;
                while (end > 0 && !char.IsLetterOrDigit(piece[end - 1]))
                    end--;

                core = piece[..end];
                tail = piece[end..];
            }

            var hex = Color.InterpolateBetween(Color.White, speechColor.Value, speechColor.Value.A).ToHex();
            body.Append($"[color={hex}]{core}[/color]");
            body.Append(tail);
        }

        plain = plainBuilder.ToString();
        return body.ToString();
    }
}
