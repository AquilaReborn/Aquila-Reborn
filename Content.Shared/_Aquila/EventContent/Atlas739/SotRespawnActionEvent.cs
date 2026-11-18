// Работа выполнена Claude (Anthropic).

using Content.Shared.Actions;

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Действие призрака "Воскреснуть на платформе": мгновенно возвращает привязанного игрока к жизни
///     на его платформе воскрешения (см. SotRespawnPad). Доступно через ActionDelay после смерти.
/// </summary>
public sealed partial class SotRespawnActionEvent : InstantActionEvent;
