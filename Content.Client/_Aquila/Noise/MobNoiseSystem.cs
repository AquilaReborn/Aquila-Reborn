using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Aquila.Noise;

public sealed partial class MobNoiseSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    private const string ShaderId = "MobNoise";

    private readonly Dictionary<EntityUid, List<LayerSnapshot>> _modified = new();

    private bool _active;
    private TimeSpan _nextScan;

    private readonly record struct LayerSnapshot(int Index, ShaderInstance? Shader, ProtoId<ShaderPrototype>? Prototype, Color Color);

    /// <summary>
    /// Накладывает шум на спрайты других мобов, пока локальный игрок находится в критическом состоянии или мёртв.
    /// </summary>
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var player = _player.LocalEntity;
        var active = player != null && (_mobState.IsCritical(player.Value) || _mobState.IsDead(player.Value));

        if (!active)
        {
            if (_active)
                Restore();

            return;
        }

        _active = true;
        if (_timing.CurTime < _nextScan)
            return;

        _nextScan = _timing.CurTime + TimeSpan.FromSeconds(1);
        Scan(player!.Value);
    }

    private void Scan(EntityUid player)
    {
        var query = EntityQueryEnumerator<SpriteComponent, MobStateComponent>();
        while (query.MoveNext(out var uid, out var sprite, out _))
        {
            if (uid == player)
                continue;

            ApplyNoise(uid, sprite);

            if (!_inventory.TryGetSlots(uid, out var slots))
                continue;

            foreach (var slot in slots)
            {
                if (!_inventory.TryGetSlotEntity(uid, slot.Name, out var item))
                    continue;

                if (TryComp<SpriteComponent>(item.Value, out var itemSprite))
                    ApplyNoise(item.Value, itemSprite);
            }
        }
    }

    private void ApplyNoise(EntityUid uid, SpriteComponent sprite)
    {
        if (_modified.ContainsKey(uid))
            return;

        var snapshots = new List<LayerSnapshot>(sprite.LayerData.Count);
        for (var i = 0; i < sprite.LayerData.Count; i++)
        {
            var layer = sprite.LayerData[i];
            snapshots.Add(new LayerSnapshot(i, layer.Shader, layer.ShaderPrototype, layer.Color));
            sprite.LayerSetShader(i, ShaderId);
            _sprite.LayerSetColor((uid, sprite), i, Color.White);
        }

        _modified[uid] = snapshots;
    }

    private void Restore()
    {
        foreach (var (uid, snapshots) in _modified)
        {
            if (TerminatingOrDeleted(uid) || !TryComp<SpriteComponent>(uid, out var sprite))
                continue;

            foreach (var snapshot in snapshots)
            {
                sprite.LayerSetShader(snapshot.Index, snapshot.Shader, snapshot.Prototype?.Id);
                _sprite.LayerSetColor((uid, sprite), snapshot.Index, snapshot.Color);
            }
        }

        _modified.Clear();
        _active = false;
    }
}
