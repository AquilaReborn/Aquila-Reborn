using Content.Shared._Aquila.Engram;
using Content.Shared.Examine;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client._Aquila.Engram;

/// <summary>
/// Энграмма собирается на сервере из манекена, и несетевые компоненты ввода и осмотра до клиента не доходят.
/// Клиент добавляет их сам, иначе игрок энграммы не может ни ходить, ни осматривать.
/// </summary>
public sealed class EngramInputSystem : EntitySystem
{
    [Dependency] private readonly InputSystem _input = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EngramComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EngramComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnStartup(Entity<EngramComponent> ent, ref ComponentStartup args)
    {
        EnsureControls(ent);
    }

    private void OnPlayerAttached(Entity<EngramComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        EnsureControls(ent);
    }

    private void EnsureControls(Entity<EngramComponent> ent)
    {
        EnsureComp<ExaminerComponent>(ent);
        EnsureComp<InputComponent>(ent).ContextName = ent.Comp.InputContext;

        if (_player.LocalEntity == ent.Owner)
            _input.SetEntityContextActive();
    }
}
