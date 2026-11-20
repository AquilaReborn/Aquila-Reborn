using Content.Server._Aquila.ProfileImage.Components;
using Content.Shared._Aquila.ProfileImage.Events;
using ServerExamineSystem = Content.Server.Examine.ExamineSystem;

namespace Content.Server._Aquila.ProfileImage.Systems;

/// <summary>
/// Answers client requests for the profile image of an examined entity.
/// </summary>
public sealed class ProfileImageSystem : EntitySystem
{
    [Dependency] private readonly ServerExamineSystem _examine = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<RequestProfileImageEvent>(OnRequestProfileImage);
    }

    private void OnRequestProfileImage(RequestProfileImageEvent ev, EntitySessionEventArgs args)
    {
        var session = args.SenderSession;

        if (session.AttachedEntity is not { Valid: true } player)
            return;

        var target = GetEntity(ev.Target);

        if (!Exists(target) || !_examine.CanExamine(player, target))
            return;

        if (_examine.IsExamineBlockedAquila(player, target))
            return;

        var png = TryComp<ProfileImageComponent>(target, out var profileImage) ? profileImage.Png : null;
        RaiseNetworkEvent(new ProfileImageResponseEvent(ev.Target, png), session);
    }
}
