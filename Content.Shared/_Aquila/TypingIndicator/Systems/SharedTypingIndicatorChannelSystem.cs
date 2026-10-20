using Content.Shared.Chat;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared._Aquila.TypingIndicator.Components;
using Content.Shared.Paper;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._Aquila.TypingIndicator.Systems;

/// <summary>
/// Подменяет индикатор печатания над персонажем в зависимости от канала чата, в который печатает игрок,
/// и показывает индикатор, пока персонаж пишет на бумаге.
/// </summary>
public abstract class SharedTypingIndicatorChannelSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttachedEvent);
        SubscribeLocalEvent<TypingIndicatorChannelComponent, PlayerDetachedEvent>(OnPlayerDetachedEvent);

        SubscribeLocalEvent<PaperComponent, BoundUIClosedEvent>(OnBoundUIClosedEvent);

        SubscribeAllEvent<TypingChannelChangedEvent>(OnTypingChannelChangedEvent);
    }

    /// <summary>
    /// Показывает или скрывает над сущностью индикатор письма на бумаге.
    /// </summary>
    /// <param name="ent">Пишущая сущность.</param>
    /// <param name="writing">Пишет ли сущность сейчас.</param>
    public void SetWriting(Entity<TypingIndicatorChannelComponent?> ent, bool writing)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (!writing)
        {
            _appearance.RemoveData(ent, TypingIndicatorChannelVisuals.Writing);
            return;
        }

        _appearance.SetData(ent, TypingIndicatorChannelVisuals.Writing, ent.Comp.WritingIndicator);
    }

    /// <summary>
    /// Возвращает индикатор печатания, который соответствует каналу чата.
    /// </summary>
    /// <param name="ent">Сущность с индикаторами каналов.</param>
    /// <param name="channel">Канал чата.</param>
    /// <returns>Прототип индикатора или <c>null</c>, если для канала используется обычный индикатор.</returns>
    public ProtoId<TypingIndicatorPrototype>? GetChannelIndicator(Entity<TypingIndicatorChannelComponent?> ent, ChatSelectChannel channel)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        if (!ent.Comp.Indicators.TryGetValue(channel, out var indicator))
            return null;

        return indicator;
    }

    /// <summary>
    /// Возвращает индикатор печатания, который сейчас показан над сущностью вместо обычного.
    /// </summary>
    /// <param name="ent">Сущность с внешним видом.</param>
    /// <returns>Прототип индикатора или <c>null</c>, если показан обычный индикатор.</returns>
    public ProtoId<TypingIndicatorPrototype>? GetCurrentIndicator(Entity<AppearanceComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return null;

        if (!_appearance.TryGetData<ProtoId<TypingIndicatorPrototype>>(ent, TypingIndicatorChannelVisuals.Indicator, out var indicator, ent.Comp))
            return null;

        return indicator;
    }

    private void OnPlayerAttachedEvent(PlayerAttachedEvent ev)
    {
        EnsureComp<TypingIndicatorChannelComponent>(ev.Entity);
    }

    private void OnPlayerDetachedEvent(Entity<TypingIndicatorChannelComponent> ent, ref PlayerDetachedEvent args)
    {
        SetCurrentIndicator(ent, null);
        SetWriting(ent.AsNullable(), false);
    }

    private void OnBoundUIClosedEvent(Entity<PaperComponent> ent, ref BoundUIClosedEvent args)
    {
        if (!PaperComponent.PaperUiKey.Key.Equals(args.UiKey))
            return;

        SetWriting(args.Actor, false);
    }

    private void OnTypingChannelChangedEvent(TypingChannelChangedEvent ev, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } uid)
            return;

        SetCurrentIndicator(uid, GetChannelIndicator(uid, ev.Channel));
    }

    private void SetCurrentIndicator(EntityUid uid, ProtoId<TypingIndicatorPrototype>? indicator)
    {
        if (indicator == null)
        {
            _appearance.RemoveData(uid, TypingIndicatorChannelVisuals.Indicator);
            return;
        }

        _appearance.SetData(uid, TypingIndicatorChannelVisuals.Indicator, indicator.Value);
    }
}
