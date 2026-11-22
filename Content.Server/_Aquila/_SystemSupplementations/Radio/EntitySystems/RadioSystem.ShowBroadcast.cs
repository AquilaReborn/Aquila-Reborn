using Content.Goobstation.Shared.StationRadio.Components;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server.Radio.EntitySystems;

public sealed partial class RadioSystem
{
    /// <summary>
    /// Канал радиовещания, которое показывают только станционные радиостанции.
    /// </summary>
    private static readonly ProtoId<RadioChannelPrototype> ShowBroadcastChannel = "RadioShow";

    /// <summary>
    /// Проверяет, что канал относится к радиовещанию.
    /// </summary>
    private static bool AquilaIsShowChannel(RadioChannelPrototype channel) => channel.ID == ShowBroadcastChannel.Id;

    /// <summary>
    /// Проверяет, что приёмник показывает радиовещание локально: станционные радиостанции.
    /// </summary>
    private bool AquilaShowReceiver(EntityUid receiver) => HasComp<StationRadioReceiverComponent>(receiver);
}
