using Content.Shared.DragDrop;
using Content.Shared.Mobs.Components;

namespace Content.Shared._Aquila.Kitchen;

/// <summary>
/// Общая часть мясорубки: решает, можно ли перетащить существо на машину. Проверки здесь только те,
/// что доступны клиенту, всё остальное перепроверяет серверная часть.
/// </summary>
public sealed class SharedMeatGrinderSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MeatGrinderComponent, CanDropTargetEvent>(OnCanDropTarget);
    }

    private void OnCanDropTarget(Entity<MeatGrinderComponent> ent, ref CanDropTargetEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.CanDrop = HasComp<MobStateComponent>(args.Dragged);
    }
}
