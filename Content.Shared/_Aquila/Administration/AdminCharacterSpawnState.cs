using Content.Shared.Eui;
using Content.Shared.Preferences;
using Robust.Shared.Serialization;

namespace Content.Shared._Aquila.Administration;

/// <summary>
/// Профиль цели и собственные профили администратора для предпросмотра.
/// </summary>
[Serializable, NetSerializable]
public sealed class AdminCharacterSpawnState : EuiStateBase
{
    /// <summary>
    /// Исходный профиль цели.
    /// </summary>
    public HumanoidCharacterProfile TargetProfile = default!;

    /// <summary>
    /// Сохранённые профили администратора по номерам слотов.
    /// </summary>
    public Dictionary<int, HumanoidCharacterProfile> Profiles = new();

    /// <summary>
    /// Сохранённые профили игрока, выбранного целью.
    /// </summary>
    public Dictionary<int, HumanoidCharacterProfile> TargetProfiles = new();

    /// <summary>
    /// Создать копию без переноса разума.
    /// </summary>
    public bool Clone;

    /// <summary>
    /// Рядом с призраком администратора есть закрытый ящик или шкаф.
    /// </summary>
    public bool CanSpawnInsideStorage;
}
