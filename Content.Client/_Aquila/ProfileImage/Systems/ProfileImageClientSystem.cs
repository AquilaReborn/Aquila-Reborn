using Content.Client.Examine;
using Content.Client._Aquila.ProfileImage.Components;
using Content.Client._Aquila.ProfileImage.UI;
using Content.Shared._Aquila.ProfileImage.Events;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.ProfileImage.Systems;

/// <summary>
/// Requests profile images of examined entities from the server.
/// </summary>
public sealed class ProfileImageClientSystem : EntitySystem
{
    [Dependency] private readonly ExamineSystem _examine = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<DetailsExamineOpenedEvent>(OnDetailsExamineOpened);
        SubscribeNetworkEvent<ProfileImageResponseEvent>(OnProfileImageResponse);
    }

    private void OnDetailsExamineOpened(DetailsExamineOpenedEvent args)
    {
        _ui.GetUIController<ProfileImageUIController>().SetDetailsMode();

        if (TryComp<ClientProfileImageComponent>(args.Target, out var cached))
        {
            _examine.ShowProfileImage(args.Target, cached.Png);
            return;
        }

        if (!_examine.CanRequestExamineAquila(args.Target))
            return;

        RaiseNetworkEvent(new RequestProfileImageEvent(GetNetEntity(args.Target)));
    }

    private void OnProfileImageResponse(ProfileImageResponseEvent args)
    {
        if (!TryGetEntity(args.Target, out var entity))
            return;

        EnsureComp<ClientProfileImageComponent>(entity.Value).Png = args.Png;
        _examine.ShowProfileImage(entity.Value, args.Png);
    }
}
