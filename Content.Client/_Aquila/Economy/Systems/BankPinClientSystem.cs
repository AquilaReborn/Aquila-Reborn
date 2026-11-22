using Content.Shared._Aquila.Economy.Events;
using Robust.Shared.Player;

namespace Content.Client._Aquila.Economy.Systems;

/// <summary>
/// Сообщает серверу, что клиент вошёл в игру и готов принять PIN счёта: только после этого сервер показывает попап,
/// иначе при позднем подключении он теряется во время загрузки.
/// </summary>
public sealed class BankPinClientSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnLocalPlayerAttached);
    }

    private void OnLocalPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RaiseNetworkEvent(new BankPinRequestEvent());
    }
}
