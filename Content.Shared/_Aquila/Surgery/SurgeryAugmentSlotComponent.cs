namespace Content.Shared._Aquila.Surgery;

/// <summary>
/// Операция работает с конкретным слотом органа, а не с первым органом нужного типа.
/// Так в одну часть тела можно поставить несколько разных аугментов, но не два одинаковых.
/// </summary>
[RegisterComponent]
public sealed partial class SurgeryAugmentSlotComponent : Component
{
    /// <summary>
    /// Слот органа, с которым работает операция.
    /// </summary>
    [DataField(required: true)]
    public string Slot = string.Empty;
}
