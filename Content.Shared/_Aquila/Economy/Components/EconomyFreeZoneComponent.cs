using Content.Shared._Aquila.Economy.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._Aquila.Economy.Components;

/// <summary>
/// Карта или грид, где экономика Aquila не действует: торгоматы бесплатные, банкоматы не работают. Нужно для ивентов со своими правилами валюты.
/// </summary>
/// <remarks>
/// Отдельного сетевого состояния с полями у компонента нет: клиенту важно только наличие компонента, по нему он считает цены в торгоматах.
/// </remarks>
[RegisterComponent, NetworkedComponent, Access(typeof(EconomyZoneSystem))]
public sealed partial class EconomyFreeZoneComponent : Component;
