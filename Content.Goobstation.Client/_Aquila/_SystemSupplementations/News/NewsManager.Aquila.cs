using Robust.Shared.Utility;

namespace Content.Goobstation.Client.News;

public sealed partial class NewsManager
{
    [Dependency] private ILocalizationManager _loc = default!;

    private const string RussianLanguage = "ru";

    private ResPath ResolveLocalizedPath(ResPath path)
    {
        if (_loc.DefaultCulture is not { TwoLetterISOLanguageName: RussianLanguage })
            return path;

        var extension = path.Extension;
        if (extension.Length == 0)
            return path;

        var localized = path.WithName($"{path.FilenameWithoutExtension}.{RussianLanguage}.{extension}");
        return _res.ContentFileExists(localized) ? localized : path;
    }
}
