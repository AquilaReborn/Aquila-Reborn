namespace Content.Shared._Aquila.Cyberware.Components;

/// <summary>
/// Имплант, который хирург не может найти и извлечь, пока его не опознал сканер имплантов.
/// Скрытые по списку сканера импланты опознаёт только глубокий скан, и то не наверняка.
/// </summary>
[RegisterComponent]
public sealed partial class IdentificationRequiredComponent : Component;
