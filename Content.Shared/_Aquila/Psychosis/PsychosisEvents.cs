using Content.Shared.Actions;
using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Psychosis;

/// <summary>
/// Просьба поменяться местами со второй личностью.
/// </summary>
public sealed partial class SplitPersonalitySwapActionEvent : InstantActionEvent;

/// <summary>
/// Ответ второй личности на просьбу поменяться местами.
/// </summary>
[Serializable, NetSerializable]
public sealed class SplitPersonalitySwapAnswerMessage(bool accepted) : EuiMessageBase
{
    public readonly bool Accepted = accepted;
}
