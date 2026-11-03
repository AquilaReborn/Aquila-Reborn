namespace Content.Shared._Aquila.Cyberware;

/// <summary>
/// Поднимается на теле, когда имплант резко нагружает организм и добавляет напряжение.
/// </summary>
[ByRefEvent]
public readonly record struct CyberwareStrainEvent(float Amount);
