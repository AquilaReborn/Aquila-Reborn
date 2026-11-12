namespace Content.Client.GameTicking.Managers;

/// <summary>
/// Хранит состояние автозаписи реплеев на служебной клиентской сущности.
/// </summary>
[RegisterComponent, Access(typeof(ClientGameTicker))]
public sealed partial class AutoReplayStateComponent : Component
{
    public bool SawPreRoundLobby;
    public bool StartPending;
    public bool StopPending;
}
