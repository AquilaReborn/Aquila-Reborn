using System.Linq;
using System.Threading.Tasks;
using Content.Shared.Preferences;
using Microsoft.EntityFrameworkCore;
using Robust.Shared.Network;

namespace Content.Server.Database;

public abstract partial class ServerDbBase
{
    private const int MaxSaveCharacterAttempts = 3;

    /// <summary>
    /// Сохраняет персонажа в слот, повторяя попытку при конфликте параллельных изменений.
    /// </summary>
    public async Task SaveCharacterSlotAsync(NetUserId userId, ICharacterProfile? profile, int slot)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                await SaveCharacterSlotOnceAsync(userId, profile, slot);
                return;
            }
            catch (DbUpdateConcurrencyException e)
            {
                if (attempt < MaxSaveCharacterAttempts)
                    continue;

                _opsLog.Error($"Failed to save character slot {slot} for {userId} after {attempt} attempts: {e.Message}");
                return;
            }
        }
    }

    private static void SyncAntagsAndTraits(Profile profile, HumanoidCharacterProfile humanoid)
    {
        var newAntags = humanoid.AntagPreferences.Select(a => a.Id).ToHashSet();
        profile.Antags.RemoveAll(a => !newAntags.Contains(a.AntagName));
        var oldAntags = profile.Antags.Select(a => a.AntagName).ToHashSet();
        profile.Antags.AddRange(
            newAntags
                .Where(a => !oldAntags.Contains(a))
                .Select(a => new Antag { AntagName = a })
        );

        var newTraits = humanoid.TraitPreferences.Select(t => t.Id).ToHashSet();
        profile.Traits.RemoveAll(t => !newTraits.Contains(t.TraitName));
        var oldTraits = profile.Traits.Select(t => t.TraitName).ToHashSet();
        profile.Traits.AddRange(
            newTraits
                .Where(t => !oldTraits.Contains(t))
                .Select(t => new Trait { TraitName = t })
        );
    }
}
