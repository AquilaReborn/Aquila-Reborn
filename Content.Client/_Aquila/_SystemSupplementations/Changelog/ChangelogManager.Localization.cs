using Robust.Shared.Localization;

namespace Content.Client.Changelog;

public sealed partial class ChangelogManager
{
    [Dependency] private readonly ILocalizationManager _changelogLocalization = default!;

    public sealed partial class ChangelogChange
    {
        /// <summary>
        /// Ключ перевода записи чейнджлога.
        /// </summary>
        [DataField("messageLoc")]
        public string? MessageLoc { get; private set; }
    }

    /// <summary>
    /// Возвращает локализованный текст записи или исходный текст, если перевода нет.
    /// </summary>
    internal string GetLocalizedMessage(ChangelogChange change)
    {
        return change.MessageLoc != null && _changelogLocalization.TryGetString(change.MessageLoc, out var localized)
            ? localized
            : change.Message;
    }
}
