using Content.Shared._Aquila.Heretic.Moon.Systems;

namespace Content.Server._Aquila.Heretic.Moon.Components;

/// <summary>
/// Обращённый вознёсшимся еретиком пути Луны: постоянный союзник в безумии.
/// </summary>
[RegisterComponent, Access(typeof(SharedMoonAscensionSystem))]
public sealed partial class MoonLunaticComponent : Component;
