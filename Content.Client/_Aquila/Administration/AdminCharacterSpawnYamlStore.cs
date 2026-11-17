using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Content.Client._Aquila.Administration;

internal sealed class AdminCharacterSpawnYamlStore<T>(
    IWritableDirProvider userData,
    ISerializationManager serialization,
    ISawmill log,
    ResPath savePath) where T : class
{
    private readonly ResPath _backupPath = new($"{savePath}.bak");
    private bool _loadedPrimary;
    private bool _loadedFromBackup;

    internal T Load(T fallback)
    {
        if (TryLoad(savePath, out var data, out _))
        {
            _loadedPrimary = true;
            _loadedFromBackup = false;
            return data;
        }

        _loadedPrimary = false;
        if (TryLoad(_backupPath, out data, out _))
        {
            _loadedFromBackup = true;
            return data;
        }

        _loadedFromBackup = false;
        return fallback;
    }

    private bool TryLoad(ResPath path, [NotNullWhen(true)] out T? data, out bool readFailed)
    {
        data = null;
        readFailed = true;
        try
        {
            string text;
            using (TextReader reader = userData.OpenText(path))
                text = reader.ReadToEnd();

            readFailed = false;
            using var yaml = new StringReader(text);
            var document = DataNodeParser.ParseYamlStream(yaml).First();
            data = serialization.Read<T>(document.Root, notNullableOverride: true);
            return data != null;
        }
        catch (FileNotFoundException) when (readFailed)
        {
            readFailed = false;
            return false;
        }
        catch (Exception e)
        {
            log.Warning($"Failed to load admin character spawn settings from {path}: {e}");
            return false;
        }
    }

    internal bool TrySave(T data)
    {
        var temporaryPath = new ResPath($"{savePath}.{Guid.NewGuid():N}.tmp");
        try
        {
            if (userData.IsDir(savePath) || userData.IsDir(_backupPath))
                throw new IOException("An admin character spawn settings path is a directory.");

            if (!_loadedPrimary && (TryLoad(savePath, out _, out var readFailed) || readFailed))
            {
                throw new IOException("The primary admin character spawn settings file cannot be safely replaced.");
            }

            var node = serialization.WriteValueAs<MappingDataNode>(data);
            using (var writer = userData.OpenWriteText(temporaryPath))
            {
                new YamlStream { new YamlDocument(node.ToYaml()) }.Save(writer, false);
            }

            if (userData.Exists(savePath))
            {
                if (_loadedFromBackup)
                {
                    userData.Delete(savePath);
                }
                else
                {
                    userData.Delete(_backupPath);
                    userData.Rename(savePath, _backupPath);
                }
            }

            userData.Rename(temporaryPath, savePath);
        }
        catch (Exception e)
        {
            log.Warning($"Failed to save admin character spawn settings to {savePath}: {e}");
            try
            {
                if (!userData.IsDir(temporaryPath))
                    userData.Delete(temporaryPath);
            }
            catch (Exception cleanupError)
            {
                log.Warning($"Failed to remove temporary admin character spawn settings: {cleanupError}");
            }

            return false;
        }

        _loadedPrimary = true;
        _loadedFromBackup = false;
        return true;
    }
}
