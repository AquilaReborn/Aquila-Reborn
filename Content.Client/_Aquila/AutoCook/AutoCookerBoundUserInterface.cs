using Content.Shared._Aquila.AutoCook;
using Content.Shared.Containers.ItemSlots;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.AutoCook;

[UsedImplicitly]
public sealed class AutoCookerBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    [ViewVariables]
    private AutoCookerWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<AutoCookerWindow>();
        _window.OnStart += (recipe, amount) => SendMessage(new AutoCookerStartMessage(recipe, amount));
        _window.OnCancel += () => SendMessage(new AutoCookerCancelMessage());
        _window.OnEject += () => SendMessage(new ItemSlotButtonPressedEvent(AutoCookerComponent.BeakerSlotId));
        _window.OnRemoveQueued += index => SendMessage(new AutoCookerRemoveQueuedMessage(index));
        _window.OnFlushBuffer += () => SendMessage(new AutoCookerFlushBufferMessage());
        _window.OnFillBuffer += () => SendMessage(new AutoCookerFillBufferMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is AutoCookerBoundUserInterfaceState cast)
            _window?.UpdateState(cast);
    }
}
