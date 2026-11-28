using Content.Client._Aquila.Heretic.Moon.Components;
using Content.Shared._Aquila.Heretic.Moon.Components;
using Content.Shared.StatusEffectNew.Components;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Containers;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Heretic.Moon.Overlays;

/// <summary>
/// Искажает только мировую сцену основного глаза игрока под действием Лунного парада.
/// </summary>
public sealed class MoonParadeFractureOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<ShaderPrototype> FractureShader = "MoonParadeFracture";
    private readonly ShaderInstance _shader;
    private readonly SharedContainerSystem _containers;
    private float _intensity;
    private float _flash;
    private TimeSpan _flashStart;
    private TimeSpan? _lastEndTime;

    /// <summary>
    /// Эффект применяется к мировой сцене до отрисовки интерфейса.
    /// </summary>
    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    /// <summary>
    /// Для отражений требуется изображение уже отрисованной сцены.
    /// </summary>
    public override bool RequestScreenTexture => true;

    /// <summary>
    /// Создаёт собственный изменяемый экземпляр шейдера.
    /// </summary>
    public MoonParadeFractureOverlay(SharedContainerSystem containers)
    {
        IoCManager.InjectDependencies(this);
        _containers = containers;
        _shader = _prototypes.Index(FractureShader).InstanceUnique();
        ZIndex = -1;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _intensity = 0f;
        _flash = 0f;
        if (_player.LocalEntity is not { } target ||
            !_entities.TryGetComponent<EyeComponent>(target, out var eye) || args.Viewport.Eye != eye.Eye ||
            !_containers.TryGetContainer(target,
                StatusEffectContainerComponent.ContainerId, out var container))
        {
            _flashStart = default;
            _lastEndTime = null;
            return false;
        }

        var now = _timing.CurTime;
        var active = false;
        foreach (var uid in container.ContainedEntities)
        {
            if (!_entities.TryGetComponent<MoonParadeFractureStatusEffectComponent>(uid, out var effect) ||
                !_entities.TryGetComponent<StatusEffectComponent>(uid, out var status) || !status.Applied || status.AppliedTo != target ||
                !_entities.TryGetComponent<MoonParadeFractureVisualsComponent>(uid, out var visuals) ||
                now < status.StartEffectTime || status.EndEffectTime is { } end && now >= end)
                continue;

            active = true;
            var fadeIn = effect.FadeIn <= TimeSpan.Zero ? 1f :
                (float) Math.Clamp((now - visuals.StartedAt).TotalSeconds / effect.FadeIn.TotalSeconds, 0, 1);
            var fadeOut = status.EndEffectTime is not { } finish || effect.FadeOut <= TimeSpan.Zero ? 1f :
                (float) Math.Clamp((finish - now).TotalSeconds / effect.FadeOut.TotalSeconds, 0, 1);
            _intensity = Math.Max(_intensity, Math.Min(fadeIn, fadeOut));

            if (_lastEndTime != status.EndEffectTime)
            {
                _lastEndTime = status.EndEffectTime;
                _flashStart = now;
            }

            var flashDuration = effect.FlashDuration <= TimeSpan.Zero ? TimeSpan.FromSeconds(0.05) : effect.FlashDuration;
            var flashElapsed = Math.Clamp((float) ((now - _flashStart).TotalSeconds / flashDuration.TotalSeconds), 0f, 1f);
            var flashStrength = Math.Clamp(effect.FlashStrength, 0f, 1f);
            _flash = Math.Max(_flash, flashStrength * (1f - flashElapsed));
        }

        if (!active)
        {
            _flashStart = default;
            _lastEndTime = null;
            _flash = 0f;
            return false;
        }

        return _intensity > 0f || _flash > 0f;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        if (ScreenTexture != null)
        {
            _shader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _shader.SetParameter("intensity", _intensity);
            handle.UseShader(_shader);
            handle.DrawRect(args.WorldBounds, Color.White);
        }

        handle.UseShader(null);

        if (_flash > 0f)
            handle.DrawRect(args.WorldBounds, new Color(1f, 1f, 1f, _flash));
    }

    protected override void DisposeBehavior()
    {
        _shader.Dispose();
        base.DisposeBehavior();
    }
}
