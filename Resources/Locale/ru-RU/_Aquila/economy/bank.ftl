bank-program-name = Банк

bank-ui-no-account = Вставьте ID-карту с банковским счётом.
bank-ui-owner = { $name } ({ $job })
bank-ui-number = Счёт № { $number }
bank-ui-balance = { $balance } { $balance ->
    [one] кредит
    [few] кредита
   *[other] кредитов
}
bank-ui-transfer-number = Номер счёта
bank-ui-transfer-amount = Сумма
bank-ui-transfer-button = Перевести
bank-ui-history-empty = Операций пока не было.
bank-ui-income = +{ $amount }
bank-ui-expense = -{ $amount }

bank-transaction-starting = Подъёмные
bank-transaction-salary = Зарплата
bank-transaction-subsidy = Субсидия Центрального командования
bank-transaction-transfer-in = Перевод от { $party }
bank-transaction-transfer-out = Перевод: { $party }

bank-notification-income = +{ $amount } { $amount ->
    [one] кредит
    [few] кредита
   *[other] кредитов
}: { $reason }

bank-transfer-no-card = В КПК нет ID-карты с банковским счётом.
bank-transfer-invalid-amount = Неверная сумма перевода.
bank-transfer-invalid-account = Счёт с таким номером не найден.
bank-transfer-insufficient = Недостаточно средств на счёте.
bank-transfer-success = Переведено { $amount } { $amount ->
    [one] кредит
    [few] кредита
   *[other] кредитов
}, получатель: { $name }.

bank-card-examine-number = Номер банковского счёта: [color=yellow]{ $number }[/color].

bank-pin-generated = PIN вашего счёта: { $pin }. Не сообщайте его никому.
bank-pin-wrong = PIN не подошёл.
bank-pin-locked = PIN заблокирован, повторите позже.
bank-pin-changed = PIN счёта изменён.
bank-pin-invalid = PIN должен состоять из четырёх цифр.
bank-pin-none = Сначала задайте PIN счёта.
bank-pin-dialog-title = PIN счёта
bank-pin-prompt = Введите четырёхзначный PIN счёта.

ent-BankAccount = банковский счёт
ent-BankCartridge = картридж "Банк"
    .desc = Программа для управления личным банковским счётом.

bank-transaction-purchase = Покупка: { $party }

vending-paid-price = { $price } кр.
vending-paid-no-account = Для оплаты нужна ID-карта с банковским счётом.
vending-paid-insufficient = Недостаточно средств, нужно { $price } кр.
vending-paid-purchased = Списано { $price } кр.

bank-transaction-bonus = Премия от { $party }
bank-transaction-fine = Штраф от { $party }
bank-transaction-budget-deposit = Пополнение бюджета: { $party }
bank-transaction-with-comment = { $reason } ({ $comment })
bank-transaction-personal-contract = Премия за личный контракт
bank-notification-charge = -{ $amount } { $amount ->
    [one] кредит
    [few] кредита
   *[other] кредитов
}: { $reason }

bank-ui-manage-comment = Причина
bank-ui-manage-bonus = Премия
bank-ui-manage-fine = Штраф
bank-ui-manage-deposit-amount = Сумма пополнения
bank-ui-manage-deposit-button = Пополнить бюджет

bank-manage-denied = У вашей ID-карты нет права выписывать премии и штрафы.
bank-manage-no-comment = Укажите причину.
bank-manage-no-station = Нет связи с бюджетом станции.
bank-manage-no-budget = В бюджете отдела недостаточно средств.
bank-manage-bonus-success = Премия { $amount } кр. выписана, получатель: { $name }.
bank-manage-fine-success = Штраф { $amount } кр. выписан, нарушитель: { $name }.
bank-manage-deposit-success = Бюджет пополнен на { $amount } кр.

bank-ui-section-account = СЧЁТ
bank-ui-section-transfer = ПЕРЕВОД
bank-ui-section-manage = ПРЕМИИ И ШТРАФЫ
bank-ui-section-history = ИСТОРИЯ ОПЕРАЦИЙ
bank-ui-credits = { $amount } кр.
bank-ui-manage-budget = Бюджет
bank-ui-manage-unknown-budget = Неизвестный бюджет
bank-ui-manage-budget-balance = В бюджете
bank-ui-manage-bonus-left = Остаток премий
bank-ui-manage-max-fine = Штраф до
vending-paid-restricted = Этот товар продаётся только сотрудникам, которым он положен по службе.
vending-paid-restricted-label = нет доступа
cargo-console-withdraw-disabled = ЦК запретило снимать бюджет станции наличными. Поощряйте сотрудников премиями.
ptl-interact-budget = Выручка ПТЛ переведена в бюджет станции.
ptl-interact-no-budget = Нет связи с бюджетом станции.

bank-transaction-fine-revoked = Отмена штрафа: { $party }
bank-ui-manage-unlimited = без ограничений
bank-ui-manage-requests = Ждут подтверждения
bank-ui-manage-fines = Последние штрафы
bank-ui-manage-approve = Подтвердить
bank-ui-manage-reject = Отклонить
bank-ui-manage-revoke = Отменить штраф
bank-ui-manage-record-bonus = Премия { $amount } кр. для { $target }
bank-ui-manage-record-fine = Штраф { $amount } кр. для { $target }
bank-ui-manage-record-details = От { $issuer }, { $budget }: { $comment }

bank-manage-request-sent = Сумма больше вашего лимита. Запрос отправлен на подтверждение старшему на смене.
bank-manage-request-approved = Запрос подтверждён.
bank-manage-request-rejected = Запрос отклонён.
bank-manage-record-missing = Запись не найдена, возможно, её уже обработали.
bank-manage-revoke-no-account = У нарушителя нет банковского счёта, отменить штраф нельзя.
bank-manage-fine-revoked = Штраф отменён, деньги возвращены нарушителю.

bank-notification-request-approved = Подтверждён ваш запрос на { $amount } кр. для { $target }.
bank-notification-request-rejected = Отклонён ваш запрос на { $amount } кр. для { $target }.

bank-radio-bonus = Премия { $amount } кр. для { $target } от { $issuer }. Причина: { $comment }
bank-radio-bonus-approved = Премия { $amount } кр. для { $target } от { $issuer }, подтвердил(а) { $manager }. Причина: { $comment }
bank-radio-fine = Штраф { $amount } кр. для { $target } от { $issuer }. Причина: { $comment }
bank-radio-fine-approved = Штраф { $amount } кр. для { $target } от { $issuer }, подтвердил(а) { $manager }. Причина: { $comment }
bank-radio-fine-revoked = Штраф { $amount } кр. для { $target } отменён, решение принял(а) { $manager }.
bank-radio-budget-deposit = Бюджет пополнен на { $amount } кр. от { $issuer }.
bank-radio-request-bonus = Запрос на премию { $amount } кр. для { $target } от { $issuer } сверх лимита. Причина: { $comment }. Нужно подтверждение в программе "Банк".
bank-radio-request-fine = Запрос на штраф { $amount } кр. для { $target } от { $issuer } больше допустимого. Причина: { $comment }. Нужно подтверждение в программе "Банк".
bank-radio-request-rejected-bonus = Запрос на премию { $amount } кр. для { $target } от { $issuer } отклонён, решение принял(а) { $manager }.
bank-radio-request-rejected-fine = Запрос на штраф { $amount } кр. для { $target } от { $issuer } отклонён, решение принял(а) { $manager }.
bank-security-alert-transaction = Банк: крупная операция на { $amount } кредитов - { $reason }.
bank-security-alert-pin-failed = Банк: несколько неудачных попыток ввода PIN. Проверьте, не потерял ли кто-то карту.
bank-manage-not-approver = Сейчас запросы подтверждает { $job }.
bank-manage-no-approver = На смене нет никого, кто может подтвердить этот запрос.

bank-party-central-command = ЦК
bank-transaction-reverted = Отмена операции, решение { $party }
bank-transaction-adjusted = Исправление баланса, решение { $party }
bank-admin-verb-panel = Банк ЦК
bank-admin-title = Банк ЦК
bank-admin-search = Поиск по имени, должности или номеру
bank-admin-refresh = Обновить
bank-admin-tab-account = Счёт
bank-admin-tab-requests = Запросы
bank-admin-tab-requests-count = Запросы ({ $count })
bank-admin-tab-fines = Штрафы
bank-admin-no-account = Выберите счёт слева.
bank-admin-no-job = без должности
bank-admin-no-requests = Запросов на подтверждение нет.
bank-admin-no-fines = Штрафов пока не было.
bank-admin-account-row = { $name }, { $job } (№ { $number }): { $balance } кр.
bank-admin-account-title = { $name }, счёт № { $number }
bank-admin-amount = Сумма
bank-admin-comment = Причина (обязательно)
bank-admin-bonus = Премия от ЦК
bank-admin-fine = Штраф от ЦК
bank-admin-set-balance = Установить баланс
bank-admin-revert = Отменить
bank-admin-revert-hint = Для отмены операции укажите причину в поле выше.
bank-admin-reset-pin = Сбросить PIN
bank-admin-reset-pin-done = PIN счёта сброшен, владелец может задать новый в банкомате.
cmd-bankpanel-desc = Открывает панель "Банк ЦК" для управления банковскими счетами.
cmd-bankpanel-help = Использование: bankpanel [номер счёта]
cmd-bankpanel-hint = [номер счёта]
bank-ui-open-account = Оформить счёт
bank-open-account-success = Счёт № { $number } оформлен на вашу ID-карту.
bank-open-account-failed = Не удалось оформить счёт: нужна ID-карта, на которую ещё не оформлен счёт.
bank-admin-verb-zone-grid-off = Экономика: отключить на этом гриде
bank-admin-verb-zone-grid-on = Экономика: включить на этом гриде
bank-admin-verb-zone-map-off = Экономика: отключить на этой карте
bank-admin-verb-zone-map-on = Экономика: включить на этой карте
bank-offline = Банковская система станции отключена.

bank-transaction-chip-withdraw = Снятие голочипом
holochip-verb-withdraw = Снять кредиты
holochip-dialog-amount = Сумма
holochip-withdrawn = Снято { $amount } кр. голочипом.
holochip-free-zone = Здесь банковская система не работает.
stack-holochip = голочип
ent-Holochip = голочип
    .desc = Чип из твёрдого света с кредитами. Приложите его к ID-карте или КПК, чтобы зачислить кредиты на счёт.
bank-transaction-chip-deposit = Внесение голочипа
holochip-deposited = Зачислено { $amount } кр. с голочипа.

bank-transaction-atm-withdraw = Снятие наличных
bank-transaction-atm-deposit = Внесение наличных
atm-ui-no-account = Нужна ID-карта с банковским счётом.
atm-ui-amount = Сумма
atm-ui-withdraw = Снять
atm-ui-deposit-hint = Чтобы внести наличные, вставьте их в банкомат.
atm-no-account = Банкомат не нашёл у вас ID-карту с банковским счётом.
atm-withdraw-limit = За один раз можно снять не больше { $limit } кредитов.
atm-withdraw-unavailable = Банкомат не смог оформить снятие наличных.
atm-deposited = Зачислено { $amount } { $amount ->
    [one] кредит
    [few] кредита
   *[other] кредитов
}.
atm-free-zone = Банкомат не обслуживает этот сектор.
atm-ui-security = Уровень защиты: { $level }
atm-security-level-low = Низкий
atm-security-level-medium = Средний
atm-security-level-high = Высокий
atm-ui-security-hint-0 = Доступ по карте: PIN не нужен.
atm-ui-security-hint-1 = Карта и PIN при покупках в автоматах и терминале.
atm-ui-security-hint-2 = Карта и PIN в банкомате и КПК.
atm-ui-change-pin = Сменить PIN
atm-ui-old-pin = Текущий PIN
atm-ui-new-pin = Новый PIN
atm-security-level-invalid = Уровень защиты бывает 0, 1 или 2.
atm-security-level-set = Уровень защиты счёта: { $level }.
ent-Atm = банкомат
    .desc = Выдаёт кредиты с банковского счёта наличными и принимает наличные на счёт.

bank-transaction-terminal-payment-out = Оплата: { $party }
bank-transaction-terminal-payment-in = Оплата: { $party }
bank-transaction-terminal-fine-out = Штраф: { $party }
bank-transaction-terminal-fine-in = Штраф: { $party }
bank-transaction-terminal-fine-refund = Возврат штрафа: { $party }
ent-PaymentTerminal = платёжный терминал
    .desc = Принимает безналичную оплату картой на месте и записывает назначение платежа в историю операций.
ent-PaymentTerminalSecurity = терминал штрафов
    .desc = Выписывает штрафы службы безопасности и принимает оплату картой в бюджет отдела.
ent-PaperReceipt = чек оплаты
    .desc = Отпечатанный терминалом оплаты чек с последней операцией.
payment-terminal-no-card = Не удалось найти у вас ID-карту с банковским счётом.
payment-terminal-card-linked = Счёт привязан к терминалу: { $name }.
payment-terminal-already-linked = К терминалу уже привязан другой счёт, перепривязка невозможна.
payment-terminal-not-owner = Это не ваш терминал.
payment-terminal-access-denied = Недостаточно доступа к этому терминалу.
payment-terminal-invoice-set = Счёт выставлен: { $amount } кр., назначение: { $purpose }.
payment-terminal-invoice-cleared = Счёт снят.
payment-terminal-paid = Оплачено { $amount } кр.
payment-terminal-fined = Штраф оплачен: { $amount } кр.
payment-terminal-fine-failed = Не удалось зачислить штраф в бюджет, деньги возвращены.
payment-terminal-cooldown = Подождите пару секунд перед повторной оплатой.
payment-terminal-no-invoice = Счёт не выставлен.
payment-terminal-no-linked-account = К терминалу не привязан счёт.
payment-terminal-no-budget = В казне станции нет счёта для штрафа.
payment-terminal-no-purpose = Укажите назначение платежа.
payment-terminal-invalid-amount = Неверная сумма: от 1 до { $max } кр.
payment-terminal-same-account = Нельзя оплатить счёт со своего же счёта.
payment-terminal-free-zone = Банковская система не обслуживает этот сектор.
payment-terminal-no-receipt = Нет последней оплаты, печатать чек не из чего.
payment-terminal-receipt-text = Сумма: { $amount } кр.
    Назначение: { $purpose }
    Плательщик: { $payer }
    Время: { $time }

payment-terminal-window-title = Платёжный терминал
payment-terminal-window-no-account = Счёт к терминалу не привязан
payment-terminal-window-linked = Привязан: { $owner }
payment-terminal-window-budget = Бюджет: { $budget }
payment-terminal-window-no-invoice = Счёт не выставлен
payment-terminal-window-awaiting = Ожидает оплаты: { $amount } кр., назначение: { $purpose }
payment-terminal-window-amount = Сумма
payment-terminal-window-purpose = Назначение
payment-terminal-window-fine-only = Терминал штрафов: оплата уходит в бюджет отдела
payment-terminal-window-link-card = Привязать карту
payment-terminal-window-set = Выставить счёт
payment-terminal-window-clear = Снять счёт
payment-terminal-window-print-receipt = Напечатать чек
payment-terminal-window-invalid-amount = Неверная сумма
payment-terminal-window-no-purpose = Назначение обязательно

bank-budget-log-vending = Выручка торгового автомата
bank-budget-log-budget-deposit = Пополнение бюджета с личного счёта
bank-budget-log-cargo-handling = Плата за доставку личного заказа
bank-budget-log-external = Внешняя операция
bank-budget-log-distribution = Распределение дохода
