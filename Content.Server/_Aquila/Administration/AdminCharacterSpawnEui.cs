using Content.Server.Administration.Systems;
using Content.Server.EUI;
using Content.Shared._Aquila.Administration;
using Content.Shared.Eui;
using Content.Shared.Preferences;
using Robust.Shared.Player;

namespace Content.Server._Aquila.Administration;

internal sealed class AdminCharacterSpawnEui(
    AdminVerbSystem system,
    EntityUid target,
    ICommonSession targetPlayer,
    AdminCharacterSpawnState state,
    EntityUid? storage,
    PlayerPreferences? preferences,
    PlayerPreferences targetPreferences) : BaseEui
{
    /// <inheritdoc/>
    public override void Opened()
    {
        StateDirty();
    }

    /// <inheritdoc/>
    public override EuiStateBase GetNewState()
    {
        return state;
    }

    /// <inheritdoc/>
    public override void HandleMessage(EuiMessageBase msg)
    {
        if (msg is not AdminCharacterSpawnRequest request)
        {
            base.HandleMessage(msg);
            return;
        }

        if (system.TrySpawnAdminCharacter(target, Player, targetPlayer, state.Clone, request, storage, preferences, targetPreferences))
            Close();
        else
            SendMessage(new AdminCharacterSpawnFailure());
    }
}
