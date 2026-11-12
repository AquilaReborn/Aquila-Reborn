namespace Content.Client.Changelog;

public sealed partial class ChangelogManager
{
    public sealed partial class ChangelogChange
    {
        /// <summary>
        /// Ключ перевода записи чейнджлога.
        /// </summary>
        [DataField("messageLoc")]
        public string? MessageLoc { get; private set; }

        internal string GetLocalizedMessage()
        {
            return MessageLoc != null && Loc.TryGetString(MessageLoc, out var localized) ? localized : Message;
        }
    }
}
