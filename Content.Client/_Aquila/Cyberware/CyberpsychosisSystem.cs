using System.Numerics;
using Content.Shared._Aquila.Cyberware;
using Content.Shared._Aquila.Cyberware.Components;
using Content.Shared._Aquila.Cyberware.Prototypes;
using Content.Shared.Humanoid;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Cyberware;

/// <summary>
/// Всё, что больной киберпсихозом видит и слышит только сам: глитчи, красный экран, враждебные облики и музыку стадии.
/// </summary>
public sealed class CyberpsychosisSystem : SharedCyberpsychosisSystem
{
    private static readonly TimeSpan DisguiseUpdateInterval = TimeSpan.FromSeconds(0.5);

    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly IOverlayManager _overlay = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private CyberpsychosisGlitchOverlay _glitchOverlay = default!;

    public override void Initialize()
    {
        base.Initialize();

        _glitchOverlay = new CyberpsychosisGlitchOverlay();

        SubscribeLocalEvent<CyberpsychosisComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<CyberpsychosisComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<CyberpsychosisComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<CyberpsychosisComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlay.RemoveOverlay(_glitchOverlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity is not { } local
            || !TryComp<CyberpsychosisComponent>(local, out var comp)
            || string.IsNullOrEmpty(comp.Stage.Id)
            || !_proto.TryIndex(comp.Stage, out var stage))
        {
            _glitchOverlay.Intensity = 0f;
            _glitchOverlay.Redness = 0f;
            return;
        }

        var now = _timing.RealTime;
        if (comp.NextGlitch == TimeSpan.Zero)
        {
            comp.NextGlitch = now + _random.Next(stage.MinGlitchDelay, stage.MaxGlitchDelay);
        }
        else if (now >= comp.NextGlitch)
        {
            comp.GlitchEnd = now + stage.GlitchDuration;
            comp.NextGlitch = now + _random.Next(stage.MinGlitchDelay, stage.MaxGlitchDelay);
        }

        var baseIntensity = comp.Suppressed ? 0f : stage.BaseGlitchIntensity;
        _glitchOverlay.Intensity = now < comp.GlitchEnd
            ? MathF.Max(stage.GlitchIntensity, baseIntensity)
            : baseIntensity;
        _glitchOverlay.Redness = comp.Suppressed ? 0f : stage.Redness;

        if (comp.Suppressed)
            StopMusic(comp);
        else
            UpdateMusic(comp, stage);

        if (now < comp.NextDisguiseUpdate)
            return;

        comp.NextDisguiseUpdate = now + DisguiseUpdateInterval;

        if (comp.Suppressed)
            ClearDisguises();
        else
            UpdateDisguises(local, stage);
    }

    private void OnStartup(Entity<CyberpsychosisComponent> ent, ref ComponentStartup args)
    {
        if (_player.LocalEntity != ent.Owner)
            return;

        _overlay.AddOverlay(_glitchOverlay);
    }

    private void OnShutdown(Entity<CyberpsychosisComponent> ent, ref ComponentShutdown args)
    {
        StopMusic(ent.Comp);

        if (_player.LocalEntity != ent.Owner)
            return;

        ClearDisguises();
        _overlay.RemoveOverlay(_glitchOverlay);
    }

    private void OnPlayerAttached(Entity<CyberpsychosisComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        ent.Comp.NextGlitch = TimeSpan.Zero;
        _overlay.AddOverlay(_glitchOverlay);
    }

    private void OnPlayerDetached(Entity<CyberpsychosisComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        StopMusic(ent.Comp);
        ClearDisguises();
        _overlay.RemoveOverlay(_glitchOverlay);
    }

    private void UpdateMusic(CyberpsychosisComponent comp, CyberpsychosisStagePrototype stage)
    {
        if (stage.Music == null)
        {
            StopMusic(comp);
            return;
        }

        if (comp.MusicStream is { } stream && !TerminatingOrDeleted(stream))
            return;

        comp.MusicStream = _audio.PlayGlobal(stage.Music, Filter.Local(), false, stage.Music.Params.WithLoop(true))?.Entity;
    }

    private void StopMusic(CyberpsychosisComponent comp)
    {
        if (comp.MusicStream is { } stream)
            _audio.Stop(stream);

        comp.MusicStream = null;
    }

    private void UpdateDisguises(EntityUid local, CyberpsychosisStagePrototype stage)
    {
        if (stage.Disguises.Count == 0)
        {
            ClearDisguises();
            return;
        }

        var targets = new List<Entity<SpriteComponent>>();
        var query = EntityQueryEnumerator<HumanoidAppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var sprite))
        {
            if (uid != local && !_container.IsEntityOrParentInContainer(uid))
                targets.Add((uid, sprite));
        }

        foreach (var target in targets)
        {
            var disguised = EnsureComp<CyberpsychosisDisguisedComponent>(target);
            if (disguised.Disguise is not { } existing || TerminatingOrDeleted(existing))
            {
                var proto = stage.Disguises[target.Owner.Id % stage.Disguises.Count];
                disguised.Disguise = Spawn(proto, new EntityCoordinates(target, Vector2.Zero));
            }

            var color = target.Comp.Color;
            if (color.A <= 0f)
                continue;

            disguised.OriginalColor = color;
            _sprite.SetColor(target.AsNullable(), color.WithAlpha(0f));
        }
    }

    private void ClearDisguises()
    {
        var disguised = new List<Entity<CyberpsychosisDisguisedComponent>>();
        var query = EntityQueryEnumerator<CyberpsychosisDisguisedComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            disguised.Add((uid, comp));
        }

        foreach (var ent in disguised)
        {
            if (ent.Comp.Disguise is { } disguise && !TerminatingOrDeleted(disguise))
                QueueDel(disguise);

            if (ent.Comp.OriginalColor is { } color && TryComp<SpriteComponent>(ent, out var sprite))
                _sprite.SetColor((ent.Owner, sprite), color);

            RemComp<CyberpsychosisDisguisedComponent>(ent);
        }
    }
}
