# Работа выполнена Claude (Anthropic).
sot-title = SoT
sot-description = Противостояние Афины и Жнецов. Раунд состоит из битв между кораблями, побеждает тот, кто выиграет больше битв.

sot-announcement-sender = Песочные часы судьбы

sot-side-athena = Афина
sot-side-reapers = Жнецы
sot-side-unknown = Неизвестно

# --- Причины завершения (в нижнем регистре: подставляются в середину фразы) ---
sot-battle-reason-destroyed = песочные часы уничтожены
sot-battle-reason-forced = битва завершена принудительно
sot-battle-reason-round-end = раунд завершился 

# --- Объявления на весь сервер ---
sot-battle-end-win = Битва №{ $number } окончена! Победитель: { $side } ({ $id }) — { $reason }.
sot-battle-end-draw = Битва №{ $number } окончена: ничья — { $reason }.

# --- Итоги раунда ---
sot-round-end-header = [bold]Битвы SoT[/bold]
sot-round-end-no-battles = В этом раунде битв не проводилось.
sot-round-end-battle = Битва №{ $number }: { $sideA } ({ $idA }) — { $sideB } ({ $idB }): { $result }. Длительность: { $duration }.
sot-round-end-result-win = победитель: { $side } ({ $id }), { $reason }
sot-round-end-result-draw = ничья, { $reason }
sot-round-end-result-interrupted = не завершена, { $reason }
sot-round-end-totals = Итого побед: Афина — { $athena }, Жнецы — { $reapers }. Ничьих: { $draws }. Не завершено: { $interrupted }.
sot-round-end-winner = Победитель раунда: [bold]{ $side }[/bold]
sot-round-end-winner-none = Победитель раунда не определён: поровну побед.
