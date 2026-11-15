using Content.Shared._Lavaland.Megafauna.Components;
using Content.Shared._Lavaland.Megafauna.Selectors;

namespace Content.Shared._Lavaland.Megafauna.Systems;

public sealed partial class MegafaunaSystem
{
    private readonly List<(TimeSpan Time, MegafaunaSelector Action)> _dueActions = new();

    private void CollectDueActions(MegafaunaAiComponent ai, TimeSpan curTime)
    {
        _dueActions.Clear();
        foreach (var (time, action) in ai.Schedule)
        {
            if (time <= curTime)
                _dueActions.Add((time, action));
        }
    }
}
