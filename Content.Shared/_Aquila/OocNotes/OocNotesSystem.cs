using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Verbs;
using Robust.Shared.Utility;

namespace Content.Shared._Aquila.OocNotes;

public sealed class OocNotesSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystemShared _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<OocNotesComponent, GetVerbsEvent<ExamineVerb>>(OnGetExamineVerbs);
    }

    public void SetNotes(EntityUid uid, string content)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            RemComp<OocNotesComponent>(uid);
            return;
        }

        var comp = EnsureComp<OocNotesComponent>(uid);
        comp.Content = content;
        Dirty(uid, comp);
    }

    private void OnGetExamineVerbs(Entity<OocNotesComponent> ent, ref GetVerbsEvent<ExamineVerb> args)
    {
        if (Identity.Name(args.Target, EntityManager) != MetaData(args.Target).EntityName)
            return;

        var user = args.User;

        var verb = new ExamineVerb
        {
            Act = () =>
            {
                var markup = new FormattedMessage();
                markup.PushColor(Color.FromHex("#8C9C90"));
                markup.AddText(Loc.GetString("ooc-notes-examine-header"));
                markup.Pop();
                markup.PushNewline();
                markup.AddText(ent.Comp.Content);
                _examine.SendExamineTooltip(user, ent, markup, false, false);
            },
            Text = Loc.GetString("ooc-notes-verb-text"),
            Category = VerbCategory.Examine,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
        };

        args.Verbs.Add(verb);
    }
}
