namespace Content.Shared._Aquila.Surgery;

/// <summary>
/// Шаг операции, который извлекает из тела все опознанные сканером подкожные импланты.
/// Неопознанные импланты хирург не находит.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryExtractImplantsStepComponent : Component;
