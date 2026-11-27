using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._Aquila.Emoting;

/// <summary>
/// Статус-эффект, который периодически заставляет существо выполнять заданную эмоцию,
/// даже если обычно оно не может этого сделать.
/// </summary>
[RegisterComponent, Access(typeof(SharedForcedEmoteStatusEffectSystem)), AutoGenerateComponentPause]
public sealed partial class ForcedEmoteStatusEffectComponent : Component
{
    /// <summary>
    /// Эмоция, которую выполняет существо.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EmotePrototype> Emote;

    /// <summary>
    /// Пауза между принудительными эмоциями.
    /// </summary>
    [DataField]
    public TimeSpan Interval = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Показывать эмоцию в чате. Если выключено, существо только издаёт звук.
    /// </summary>
    [DataField]
    public bool ShowInChat = true;

    /// <summary>
    /// Момент, когда существо выполнит эмоцию в следующий раз.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextEmote;
}
