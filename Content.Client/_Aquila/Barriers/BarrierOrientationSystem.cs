using Content.Shared.Aquila.Barriers;
using Content.Shared.Input;
using Content.Shared.Popups;
using Robust.Client.Player;
using Robust.Shared.Input.Binding;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using System.Numerics;
using Robust.Shared.Map;
using Content.Shared.Coordinates;

namespace Content.Client.Aquila.Barriers;

public sealed class BarrierOrientationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.MouseMiddle, new PointerInputCmdHandler(MiddleMousePressed))
            .Register<BarrierOrientationSystem>();
    }

    public override void Shutdown()
    {
        base.Shutdown();
        CommandBinds.Unregister<BarrierOrientationSystem>();
    }

    private bool MiddleMousePressed(ICommonSession? playerSession, EntityCoordinates coordinates, EntityUid entity)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        var enumerator = EntityQueryEnumerator<BarrierOrientationComponent>();
        while (enumerator.MoveNext(out var uid, out var component))
        {
            var newState = !component.IsVertical;

            RaiseNetworkEvent(new BarrierOrientationToggleEvent { Action = GetNetEntity(uid) });

            _popup.PopupCursor(newState ? Loc.GetString("barrier-orientation-vertical") : Loc.GetString("barrier-orientation-horizontal"));
        }

        return false;
    }
}
