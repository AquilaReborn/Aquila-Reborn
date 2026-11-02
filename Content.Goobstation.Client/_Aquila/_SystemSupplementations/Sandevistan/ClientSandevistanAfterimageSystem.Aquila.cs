using Content.Goobstation.Shared.Sandevistan;
using Robust.Client.GameObjects;
using Robust.Shared.Timing;

namespace Content.Goobstation.Client.Sandevistan;

public sealed partial class ClientSandevistanAfterimageSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private const float AfterimageAlpha = 0.7f;

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        var now = _timing.CurTime;
        var query = EntityQueryEnumerator<SandevistanAfterimageComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out var afterimage, out var sprite))
        {
            if (afterimage.Lifetime <= 0f)
                continue;

            var progress = (float) (now - afterimage.SpawnTime).TotalSeconds / afterimage.Lifetime;
            var color = sprite.Color;
            _sprite.SetColor((uid, sprite), color.WithAlpha(AfterimageAlpha * Math.Clamp(1f - progress, 0f, 1f)));
        }
    }
}
