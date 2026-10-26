using Content.Goobstation.Shared.Changeling.Components;
using Content.Server.Chat.Systems;
using Content.Shared.Chat;
using Content.Shared.Chat.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Server._Aquila.Changeling;

public sealed class ChangelingHorrorEmoteSystem : EntitySystem
{
    private static readonly ProtoId<EmoteSoundsPrototype> HorrorSounds = "ChangelingHorror";

    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, EmoteEvent>(OnEmote);
    }

    private void OnEmote(Entity<ChangelingComponent> ent, ref EmoteEvent args)
    {
        if (args.Handled || !args.Emote.Category.HasFlag(EmoteCategory.Vocal))
            return;

        args.Handled = _chat.TryPlayEmoteSound(ent, _proto.Index(HorrorSounds), args.Emote);
    }
}
