// Работа выполнена Claude (Anthropic).

namespace Content.Shared._Aquila.EventContent.Atlas739;

/// <summary>
///     Общий ID, связывающий сущность (например, песочные часы) с шаттлом (гридом).
///     Можно повесить на песочные часы — тогда шаттлом считается грид, на котором они стоят,
///     либо прямо на сам грид (например, через маппинг).
/// </summary>
[RegisterComponent]
public sealed partial class ShuttleLinkComponent : Component
{
    /// <summary>Уникальный ID связи, например "athena".</summary>
    [DataField(required: true)]
    public string Id = string.Empty;
}
