using Content.Shared.Chat;
using Content.Shared.EntityEffects;
using Content.Shared.Speech;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._Aquila.Cyberware.EntityEffects;

/// <inheritdoc cref="EntityEffectSystem{T,TEffect}"/>
public sealed partial class ForceSayEntityEffectSystem : EntityEffectSystem<SpeechComponent, ForceSay>
{
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    protected override void Effect(Entity<SpeechComponent> entity, ref EntityEffectEvent<ForceSay> args)
    {
        if (_net.IsClient || args.Effect.Messages.Count == 0)
            return;

        var message = Loc.GetString(_random.Pick(args.Effect.Messages));
        _chat.TrySendInGameICMessage(entity.Owner,
            message,
            InGameICChatType.Speak,
            false,
            checkRadioPrefix: false,
            ignoreActionBlocker: true,
            forced: true);
    }
}

/// <summary>
/// Заставляет существо произнести вслух одну случайную фразу из списка.
/// </summary>
public sealed partial class ForceSay : EntityEffectBase<ForceSay>
{
    /// <summary>
    /// Фразы, из которых выбирается одна.
    /// </summary>
    [DataField(required: true)]
    public List<LocId> Messages = new();
}
