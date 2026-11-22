using Content.Client._Aquila.Economy.UI;
using Content.Shared._Aquila.Economy.BUI;
using Robust.Client.UserInterface;

namespace Content.Client._Aquila.Economy.BUI;

public sealed class PaymentTerminalBoundUserInterface(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private PaymentTerminalWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<PaymentTerminalWindow>();
        _window.OnSetInvoice += (amount, purpose) => SendMessage(new TerminalSetInvoiceMessage(amount, purpose));
        _window.OnClearInvoice += () => SendMessage(new TerminalClearInvoiceMessage());
        _window.OnPrintReceipt += () => SendMessage(new TerminalPrintReceiptMessage());
        _window.OnLinkCard += () => SendMessage(new TerminalLinkCardMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is PaymentTerminalUiState terminalState)
            _window?.UpdateState(terminalState);
    }
}
