# Работа выполнена Claude (Anthropic).
# UI песочных часов и фразы, которые часы произносят в местный чат.

# --- Окно ---
sot-hourglass-window-title = Песочные часы
sot-hourglass-ship = { $side }: корабль { $id }

# --- Состояние корабля ---
sot-hourglass-phase-Idle = Корабль не готов к битве
sot-hourglass-phase-Countdown = Вылет через { $seconds } с
sot-hourglass-phase-Waiting = Корабль в FTL, ждёт начала битвы
sot-hourglass-phase-Battle = Идёт битва
sot-hourglass-phase-FtlBusy = Корабль в FTL-прыжке или на перезарядке
sot-hourglass-phase-Unavailable = Недоступно

# --- Голоса ---
sot-hourglass-votes-ready = Голосов за готовность: { $votes } из { $required }
sot-hourglass-votes-cancel = Голосов за отмену готовности: { $votes } из { $required }

sot-hourglass-button-ready = Проголосовать: готовы к битве
sot-hourglass-button-ready-revoke = Отозвать голос за готовность
sot-hourglass-button-cancel = Проголосовать: отменить готовность
sot-hourglass-button-cancel-revoke = Отозвать голос за отмену

# --- Подсказки ---
sot-hourglass-info-Idle = Когда нужное число живых членов экипажа на борту проголосует «готовы», часы объявят вылет, и через { $seconds } секунд корабль уйдёт в FTL.
sot-hourglass-info-Countdown = Решающий голос получен. Чтобы остановить вылет, нужно столько же голосов за отмену, сколько было нужно за готовность.
sot-hourglass-info-Waiting = Корабль ждёт начала битвы. Чтобы вернуться на прежнюю позицию, проголосуйте за отмену готовности.
sot-hourglass-info-Battle = Идёт битва, голосовать нельзя.
sot-hourglass-info-FtlBusy = Корабль выполняет FTL-прыжок или перезаряжается. Голосовать пока нельзя.
sot-hourglass-info-Unavailable = Голосование сейчас недоступно: команды SoT отключены для текущего пресета, либо часы стоят не на шаттле.

# --- Всплывающие подсказки ---
sot-hourglass-popup-cannot-vote = Голосовать могут только живые члены экипажа на борту корабля.
sot-hourglass-popup-unavailable = Сейчас голосование недоступно.

# --- Фразы часов в местный чат ---
sot-hourglass-say-countdown = Экипаж готов! Вылет через { $seconds } секунд.
sot-hourglass-say-countdown-cancelled = Готовность отменена. Вылет отменён.
sot-hourglass-say-departing = Вылет!
sot-hourglass-say-depart-failed = Вылет невозможен: { $error }
sot-hourglass-say-return = Готовность отменена. Корабль возвращается на прежнюю позицию.
