bank-program-name = Bank

bank-ui-no-account = Insert an ID card with a bank account.
bank-ui-owner = { $name } ({ $job })
bank-ui-number = Account #{ $number }
bank-ui-balance = { $balance } { $balance ->
    [one] credit
   *[other] credits
}
bank-ui-transfer-number = Account number
bank-ui-transfer-amount = Amount
bank-ui-transfer-button = Send
bank-ui-history-empty = No transactions yet.
bank-ui-income = +{ $amount }
bank-ui-expense = -{ $amount }

bank-transaction-starting = Starting funds
bank-transaction-salary = Salary
bank-transaction-subsidy = Central Command subsidy
bank-transaction-transfer-in = Transfer from { $party }
bank-transaction-transfer-out = Transfer to { $party }

bank-notification-income = +{ $amount } { $amount ->
    [one] credit
   *[other] credits
}: { $reason }

bank-transfer-no-card = There is no ID card with a bank account in the PDA.
bank-transfer-invalid-amount = Invalid transfer amount.
bank-transfer-invalid-account = No account with this number was found.
bank-transfer-insufficient = Insufficient funds.
bank-transfer-success = Transferred { $amount } { $amount ->
    [one] credit
   *[other] credits
} to { $name }.

bank-card-examine-number = Bank account number: [color=yellow]{ $number }[/color].

bank-pin-generated = Your account PIN: { $pin }. Do not tell it to anyone.
bank-pin-wrong = Wrong account PIN.
bank-pin-changed = Account PIN changed.
bank-pin-invalid = The PIN must consist of four digits.
bank-pin-none = Set an account PIN first.
bank-pin-dialog-title = Account PIN
bank-pin-prompt = Enter the four-digit account PIN.

ent-BankAccount = bank account
ent-BankCartridge = bank cartridge
    .desc = A program for managing a personal bank account.

bank-transaction-purchase = Purchase: { $party }

vending-paid-price = { $price } cr.
vending-paid-no-account = An ID card with a bank account is required to pay.
vending-paid-insufficient = Insufficient funds, { $price } cr. required.
vending-paid-purchased = Charged { $price } cr.

bank-transaction-bonus = Bonus from { $party }
bank-transaction-fine = Fine from { $party }
bank-transaction-budget-deposit = Department budget top-up: { $party }
bank-transaction-with-comment = { $reason } ({ $comment })
bank-transaction-personal-contract = Personal contract premium
bank-notification-charge = -{ $amount } { $amount ->
    [one] credit
   *[other] credits
}: { $reason }

bank-ui-manage-comment = Reason
bank-ui-manage-bonus = Bonus
bank-ui-manage-fine = Fine
bank-ui-manage-deposit-amount = Top-up amount
bank-ui-manage-deposit-button = Top up budget

bank-manage-denied = Your ID card is not authorized to issue bonuses and fines.
bank-manage-no-comment = Specify a reason.
bank-manage-no-station = No connection to the station budget.
bank-manage-no-budget = The department budget has insufficient funds.
bank-manage-bonus-success = Bonus of { $amount } cr. issued to { $name }.
bank-manage-fine-success = Fine of { $amount } cr. issued to { $name }.
bank-manage-deposit-success = Budget topped up by { $amount } cr.

bank-ui-section-account = ACCOUNT
bank-ui-section-transfer = TRANSFER
bank-ui-section-manage = BONUSES AND FINES
bank-ui-section-history = TRANSACTION HISTORY
bank-ui-credits = { $amount } cr.
bank-ui-manage-budget = Budget
bank-ui-manage-unknown-budget = Unknown budget
bank-ui-manage-budget-balance = Budget balance
bank-ui-manage-bonus-left = Bonuses left
bank-ui-manage-max-fine = Fines up to
vending-paid-restricted = This item is sold only to staff authorized to carry it.
vending-paid-restricted-label = no access
cargo-console-withdraw-disabled = Central Command has prohibited withdrawing the station budget as cash. Reward crew with bonuses instead.
ptl-interact-budget = PTL revenue transferred to the station budget.
ptl-interact-no-budget = No connection to the station budget.

bank-transaction-fine-revoked = Fine revoked: { $party }
bank-ui-manage-unlimited = unlimited
bank-ui-manage-requests = Awaiting approval
bank-ui-manage-fines = Recent fines
bank-ui-manage-approve = Approve
bank-ui-manage-reject = Reject
bank-ui-manage-revoke = Revoke fine
bank-ui-manage-record-bonus = Bonus of { $amount } cr. for { $target }
bank-ui-manage-record-fine = Fine of { $amount } cr. for { $target }
bank-ui-manage-record-details = By { $issuer }, { $budget }: { $comment }

bank-manage-request-sent = The amount exceeds your limit. The request has been sent to the most senior head on shift for approval.
bank-manage-request-approved = Request approved.
bank-manage-request-rejected = Request rejected.
bank-manage-record-missing = Record not found, it may have already been handled.
bank-manage-revoke-no-account = The offender has no bank account, the fine cannot be revoked.
bank-manage-fine-revoked = Fine revoked, the money was returned to the offender.

bank-notification-request-approved = Your request of { $amount } cr. for { $target } was approved.
bank-notification-request-rejected = Your request of { $amount } cr. for { $target } was rejected.

bank-radio-bonus = Bonus of { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }
bank-radio-bonus-approved = Bonus of { $amount } cr. for { $target } from { $issuer }, approved by { $manager }. Reason: { $comment }
bank-radio-fine = Fine of { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }
bank-radio-fine-approved = Fine of { $amount } cr. for { $target } from { $issuer }, approved by { $manager }. Reason: { $comment }
bank-radio-fine-revoked = Fine of { $amount } cr. for { $target } revoked by { $manager }.
bank-radio-budget-deposit = Budget topped up by { $amount } cr. from { $issuer }.
bank-radio-request-bonus = Over-limit bonus request: { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }. Approval required in the "Bank" program.
bank-radio-request-fine = Oversized fine request: { $amount } cr. for { $target } from { $issuer }. Reason: { $comment }. Approval required in the "Bank" program.
bank-radio-request-rejected-bonus = Bonus request of { $amount } cr. for { $target } from { $issuer } rejected by { $manager }.
bank-radio-request-rejected-fine = Fine request of { $amount } cr. for { $target } from { $issuer } rejected by { $manager }.
bank-security-alert-transaction = Bank: large transaction of { $amount } credits - { $reason }.
bank-security-alert-pin-failed = Bank: several failed PIN entry attempts. Check if someone has lost their card.
bank-manage-not-approver = Requests are currently approved by the { $job }.
bank-manage-no-approver = There is no one on shift who can approve this request.

bank-party-central-command = CentCom
bank-transaction-reverted = Transaction reverted by { $party }
bank-transaction-adjusted = Balance corrected by { $party }
bank-admin-verb-panel = CentCom Bank
bank-admin-title = CentCom Bank
bank-admin-search = Search by name, job or number
bank-admin-refresh = Refresh
bank-admin-tab-account = Account
bank-admin-tab-requests = Requests
bank-admin-tab-requests-count = Requests ({ $count })
bank-admin-tab-fines = Fines
bank-admin-no-account = Select an account on the left.
bank-admin-no-job = no job
bank-admin-no-requests = No requests awaiting approval.
bank-admin-no-fines = No fines yet.
bank-admin-account-row = { $name }, { $job } (#{ $number }): { $balance } cr.
bank-admin-account-title = { $name }, account #{ $number }
bank-admin-amount = Amount
bank-admin-comment = Reason (required)
bank-admin-bonus = CentCom bonus
bank-admin-fine = CentCom fine
bank-admin-set-balance = Set balance
bank-admin-revert = Revert
bank-admin-revert-hint = To revert a transaction, enter a reason in the field above.
bank-admin-reset-pin = Reset PIN
bank-admin-reset-pin-done = Account PIN reset, the owner can set a new one at an ATM.
cmd-bankpanel-desc = Opens the CentCom Bank panel for managing bank accounts.
cmd-bankpanel-help = Usage: bankpanel [account number]
cmd-bankpanel-hint = [account number]
bank-ui-open-account = Open an account
bank-open-account-success = Account #{ $number } has been opened on your ID card.
bank-open-account-failed = Could not open an account: an ID card without an existing account is required.
bank-admin-verb-zone-grid-off = Economy: disable on this grid
bank-admin-verb-zone-grid-on = Economy: enable on this grid
bank-admin-verb-zone-map-off = Economy: disable on this map
bank-admin-verb-zone-map-on = Economy: enable on this map
bank-offline = The station banking system is offline.

bank-transaction-chip-withdraw = Holochip withdrawal
holochip-verb-withdraw = Withdraw credits
holochip-dialog-amount = Amount
holochip-withdrawn = Withdrew { $amount } cr. as a holochip.
holochip-free-zone = The banking system does not work here.
stack-holochip = holochip
ent-Holochip = holochip
    .desc = A hard-light chip that stores credits. Use it on an ID card or PDA to deposit them.
bank-transaction-chip-deposit = Holochip deposit
holochip-deposited = Deposited { $amount } cr. from the holochip.

bank-transaction-atm-withdraw = Cash withdrawal
bank-transaction-atm-deposit = Cash deposit
atm-ui-no-account = An ID card with a bank account is required.
atm-ui-amount = Amount
atm-ui-withdraw = Withdraw
atm-ui-deposit-hint = Insert cash into the ATM to deposit it.
atm-no-account = The ATM could not find an ID card with a bank account on you.
atm-withdraw-limit = You can withdraw no more than { $limit } credits at once.
atm-withdraw-unavailable = The ATM could not process the cash withdrawal.
atm-deposited = Deposited { $amount } { $amount ->
    [one] credit
   *[other] credits
}.
atm-free-zone = This ATM does not serve this sector.
atm-ui-security = Security level: { $level }
atm-security-level-low = Low
atm-security-level-medium = Medium
atm-security-level-high = High
atm-ui-security-hint-0 = Card only: no PIN required.
atm-ui-security-hint-1 = Card and PIN for purchases at vending machines and the payment terminal.
atm-ui-security-hint-2 = Card and PIN at ATMs and PDAs.
atm-ui-change-pin = Change PIN
atm-ui-old-pin = Current PIN
atm-ui-new-pin = New PIN
atm-security-level-invalid = The security level must be 0, 1 or 2.
atm-security-level-set = Account security level: { $level }.

ent-Atm = ATM
    .desc = Withdraws credits from your bank account as cash and accepts cash deposits.

bank-transaction-terminal-payment-out = Payment: { $party }
bank-transaction-terminal-payment-in = Payment: { $party }
bank-transaction-terminal-fine-out = Fine: { $party }
bank-transaction-terminal-fine-in = Fine: { $party }
bank-transaction-terminal-fine-refund = Fine refund: { $party }
ent-PaymentTerminal = payment terminal
    .desc = Takes card payments on the spot and records the payment purpose in the transaction history.
ent-PaymentTerminalSecurity = fine terminal
    .desc = Issues Security fines and takes card payment into the department budget.
ent-PaperReceipt = payment receipt
    .desc = A receipt printed by a payment terminal with the last transaction.
payment-terminal-no-card = No ID card with a bank account was found on you.
payment-terminal-card-linked = Account linked to the terminal: { $name }.
payment-terminal-already-linked = Another account is already linked to this terminal, relinking is not possible.
payment-terminal-not-owner = This is not your terminal.
payment-terminal-access-denied = You do not have access to this terminal.
payment-terminal-invoice-set = Invoice set: { $amount } cr., purpose: { $purpose }.
payment-terminal-invoice-cleared = Invoice cleared.
payment-terminal-paid = Paid { $amount } cr.
payment-terminal-fined = Fine paid: { $amount } cr.
payment-terminal-fine-failed = The fine could not be added to the budget, the money was returned.
payment-terminal-cooldown = Wait a couple of seconds before paying again.
payment-terminal-no-invoice = No invoice has been set.
payment-terminal-no-linked-account = No account is linked to this terminal.
payment-terminal-no-budget = The station treasury has no account for fines.
payment-terminal-no-purpose = Specify the payment purpose.
payment-terminal-invalid-amount = Invalid amount: from 1 to { $max } cr.
payment-terminal-same-account = You cannot pay an invoice linked to your own account.
payment-terminal-free-zone = The banking system does not serve this sector.
payment-terminal-no-receipt = There is no recent payment to print a receipt for.
payment-terminal-receipt-text = Amount: { $amount } cr.
    Purpose: { $purpose }
    Payer: { $payer }
    Time: { $time }

payment-terminal-window-title = Payment terminal
payment-terminal-window-no-account = No account is linked to the terminal
payment-terminal-window-linked = Linked: { $owner }
payment-terminal-window-budget = Budget: { $budget }
payment-terminal-window-no-invoice = No invoice is set
payment-terminal-window-awaiting = Awaiting payment: { $amount } cr., purpose: { $purpose }
payment-terminal-window-amount = Amount
payment-terminal-window-purpose = Purpose
payment-terminal-window-fine-only = Fine terminal: payments go to the department budget
payment-terminal-window-link-card = Link card
payment-terminal-window-set = Set invoice
payment-terminal-window-clear = Clear invoice
payment-terminal-window-print-receipt = Print receipt
payment-terminal-window-invalid-amount = Invalid amount
payment-terminal-window-no-purpose = Purpose is required

bank-budget-log-vending = Vending machine revenue
bank-budget-log-budget-deposit = Budget top-up from a personal account
bank-budget-log-cargo-handling = Personal cargo order delivery fee
bank-budget-log-external = External operation
bank-budget-log-distribution = Revenue distribution
