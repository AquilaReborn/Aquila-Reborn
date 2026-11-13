using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration;

/// <summary>
/// Сообщает окну, что сервер отклонил запрос на создание персонажа.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminCharacterSpawnFailure : EuiMessageBase;
