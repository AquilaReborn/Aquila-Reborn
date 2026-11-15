using Content.Shared.CharacterInfo;
using Robust.Shared.Network;

namespace Content.Client.CharacterInfo;

public sealed partial class CharacterInfoSystem
{
    [Dependency] private readonly INetManager _connection = default!;

    private void RequestCharacterInfoAquila()
    {
        if (_connection.IsConnected && _players.LocalEntity is { } entity)
            RaiseNetworkEvent(new RequestCharacterInfoEvent(GetNetEntity(entity)));
    }

    private bool TryGetCharacterInfoEntityAquila(NetEntity netEntity, out EntityUid entity)
    {
        entity = default;
        if (!TryGetEntity(netEntity, out var local) || local is not { } uid || !Exists(uid))
            return false;

        entity = uid;
        return true;
    }
}
