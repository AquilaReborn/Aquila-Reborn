## Сканер имплантов

cyberware-scanner-being-scanned = {CAPITALIZE($user)} водит над вами сканером.
cyberware-scanner-adapting = Организм ещё привыкает к недавно установленным имплантам.

cyberware-scanner-ui-title = Сканер имплантов
cyberware-scanner-ui-empty = Проведите сканером по человеку, чтобы увидеть результат.
cyberware-scanner-ui-scan-quick = Быстрое сканирование
cyberware-scanner-ui-scan-deep = Глубокое сканирование
cyberware-scanner-ui-load = Кибернагрузка
cyberware-scanner-ui-risk = Риск:
cyberware-scanner-ui-risk-value = {$risk} из {$max} ({$percent}%)
cyberware-scanner-ui-count = Имплантов:
cyberware-scanner-ui-level = Уровень:
cyberware-scanner-ui-implants = Импланты
cyberware-scanner-ui-no-implants = Опознанных подкожных имплантов нет.
cyberware-scanner-ui-psychosis = Киберпсихоз
cyberware-scanner-ui-stage = Стадия: {$stage}
cyberware-scanner-ui-cure = Лечение: {$percent}%

cyberware-scanner-level-none = отсутствует
cyberware-scanner-level-low = низкая
cyberware-scanner-level-elevated = повышенная
cyberware-scanner-level-high = высокая
cyberware-scanner-level-critical = критическая

## Нейроадаптин

reagent-name-neuroadaptin = нейроадаптин
reagent-desc-neuroadaptin = Помогает нервной системе гораздо быстрее привыкнуть к недавно установленным имплантам. Глушит симптомы киберпсихоза и на время снижает риск, что он станет тяжелее. При передозировке токсичен.

entity-effect-guidebook-adjust-cyber-strain =
    { $chance ->
        [1] { $deltasign ->
                [1] Усиливает
                *[-1] Снимает
            }
        *[other] { $deltasign ->
                [1] усилить
                *[-1] снять
            }
    } напряжение от недавно установленных имплантов

## Ферроанима

reagent-name-ferroanima = ферроанима
reagent-desc-ferroanima = Дорогой препарат, который навсегда укрепляет связи между нервной системой и железом, так что организм выдерживает больше имплантов. Действует постепенно и до определённого предела. При передозировке токсичен и резко перегружает импланты.

entity-effect-guidebook-adjust-cyber-tolerance =
    { $chance ->
        [1] Навсегда повышает
        *[other] навсегда повысить
    } переносимость имплантов


## Сущности

ent-CyberwareScanner = сканер имплантов
    .desc = Ручной сканер, который находит импланты под кожей и оценивает, насколько сильно они нагружают организм.

## Киберпсихоз

cyberpsychosis-instability-enter = Что-то не так. Железо внутри будто живёт своей жизнью, а по краям зрения бегут помехи.
cyberpsychosis-instability-exit = Помехи стихают, организм наконец свыкся с имплантами.
cyberpsychosis-instability-message-1 = На долю секунды картинка перед глазами рассыпается на пиксели.
cyberpsychosis-instability-message-2 = Под кожей что-то тихо гудит, и этот гул никак не уходит.
cyberpsychosis-instability-message-3 = Пальцы на мгновение перестают слушаться, будто сигнал до них не дошёл.
cyberpsychosis-instability-message-4 = В ушах щёлкает, как в плохом динамике.
cyberpsychosis-instability-message-5 = Вам кажется, что импланты работают чуть быстрее, чем вы успеваете думать.
cyberpsychosis-instability-tremor = Руки начинает мелко трясти, и унять дрожь не получается.

cyberpsychosis-rejection-enter = Тело отвергает железо. Во рту привкус металла, а голова гудит так, что сложно разобрать собственные мысли.
cyberpsychosis-rejection-exit = Отторжение отступает, но по краям зрения всё ещё пробегают помехи.
cyberpsychosis-rejection-cough = Вы заходитесь кашлем, и на губах остаётся кровь.
cyberpsychosis-rejection-spasm = Тело сводит судорогой, импланты будто бьют током изнутри!
cyberpsychosis-rejection-message-1 = Где-то совсем рядом звучит голос, но рядом никого нет.
cyberpsychosis-rejection-message-2 = Металл под кожей горит, словно его раскалили.
cyberpsychosis-rejection-message-3 = Вы не помните, как оказались здесь.
cyberpsychosis-rejection-message-4 = Мысли путаются, перебиваемые чужим цифровым шумом.
cyberpsychosis-rejection-message-5 = Собственное отражение кажется вам чужим и слишком механическим.

entity-effect-guidebook-bleed-out =
    { $chance ->
        [1] Выплёскивает
        *[other] выплеснуть
    } { $amount } ед. крови наружу

cyberpsychosis-breakdown-enter = Что-то внутри щёлкает и ломается. Мир заливает красным, а люди вокруг больше не похожи на людей. Вы киберпсихопат: железо управляет вами, и экипаж со службой безопасности вправе остановить вас любой ценой.
cyberpsychosis-breakdown-examine = [color=red]Пустой немигающий взгляд смотрит сквозь вас, а импланты под кожей мерцают и искрят. Похоже, этот человек совсем не в себе.[/color]
cyberpsychosis-breakdown-rampage = Тело больше не слушается. Руки сами тянутся к оружию!

cyberpsychosis-breakdown-warcry-1 = Вы все... вы все одинаковые! Не подходите!
cyberpsychosis-breakdown-warcry-2 = Я вижу, кто вы на самом деле!
cyberpsychosis-breakdown-warcry-3 = Цель обнаружена. Устранить.
cyberpsychosis-breakdown-warcry-4 = Хватит! Хватит смотреть на меня!

cyberpsychosis-breakdown-paranoia-1 = Они следят за мной. Все они.
cyberpsychosis-breakdown-paranoia-2 = Это не ваше лицо. Снимите его!
cyberpsychosis-breakdown-paranoia-3 = Кто вас прислал?
cyberpsychosis-breakdown-paranoia-4 = Я слышу, как вы шепчетесь у меня за спиной.

cyberpsychosis-breakdown-machine-1 = Ошибка... ошибка... перезагрузка...
cyberpsychosis-breakdown-machine-2 = Мясо слабое. Железо вечно.
cyberpsychosis-breakdown-machine-3 = Я больше не чувствую своих рук. Только сигнал.
cyberpsychosis-breakdown-machine-4 = Шум... в голове столько шума...

ent-CyberpsychosisDisguiseOperative = враждебный оперативник
    .desc = { "" }
ent-CyberpsychosisDisguiseHereticRust = враждебный еретик
    .desc = { "" }
ent-CyberpsychosisDisguiseHereticVoid = { ent-CyberpsychosisDisguiseHereticRust }
    .desc = { "" }
ent-CyberpsychosisDisguiseRevenant = враждебный ревенант
    .desc = { "" }
ent-CyberpsychosisDisguiseNinja = враждебный ниндзя
    .desc = { "" }
ent-CyberpsychosisDisguiseWizard = враждебный волшебник
    .desc = { "" }
ent-CyberpsychosisDisguiseZombie = враждебный зомби
    .desc = { "" }

## Лечение киберпсихоза

cyberpsychosis-stage-instability = нестабильность
cyberpsychosis-stage-rejection = отторжение
cyberpsychosis-stage-breakdown = срыв

cyberpsychosis-suppressed = Шум в голове стихает, будто кто-то убавил громкость. Можно снова думать.
cyberpsychosis-suppression-ended = Лекарство отпускает, и шум в голове возвращается.
cyberpsychosis-therapy-blocked = Лекарство не справляется: железа в теле слишком много.
cyberpsychosis-cured = Помехи гаснут одна за другой. Вы снова чувствуете себя собой, а импланты просто часть тела.

reagent-name-cybersynaptin = киберсинаптин
reagent-desc-cybersynaptin = Восстанавливает связь между нервной системой и имплантами. Если продержать его в крови пару минут без перерыва, излечивает киберпсихоз, но только когда кибернагрузка ниже критической. Вызывает сонливость, при передозировке токсичен.

entity-effect-guidebook-suppress-cyberpsychosis =
    { $chance ->
        [1] Глушит
        *[other] заглушить
    } приступы киберпсихоза
entity-effect-guidebook-cyberpsychosis-resistance =
    { $chance ->
        [1] Снижает
        *[other] снизить
    } на {$percent}% шанс, что киберпсихоз начнётся или станет тяжелее, на {NATURALFIXED($time, 0)} сек.
entity-effect-guidebook-cyberpsychosis-therapy =
    { $chance ->
        [1] Лечит
        *[other] лечить
    } киберпсихоз, если кибернагрузка ниже критической

cyberpsychosis-admin-verb-cure = Вылечить киберпсихоз

## Второй слот руки и ЭМИ

surgery-popup-augment-duplicate = В этой руке уже стоит такой же аугмент.

cyberware-action-disabled = Имплант не отвечает: его вырубило ЭМИ.
cyberware-emp-shield-broken = Импланты сбоят и искрят, экранирование пробито. Следующий импульс их отключит!
cyberware-emp-disabled = Импланты отключаются, и по телу проходит болезненный разряд!

ent-SurgeryOpenArmAugmentSecondary = Открыть второй разрез для импланта руки
    .desc = { ent-SurgeryOpenCavityBase.desc }
ent-SurgeryInsertArmAugmentSecondary = Установить второй имплант руки
    .desc = { ent-SurgeryInstallAugmentBase.desc }
ent-SurgeryRemoveArmAugmentSecondary = Удалить второй имплант руки
    .desc = { ent-SurgeryRemoveAugmentBase.desc }

## Средства СБ против имплантов

cyberware-action-blocked = Ошейник глушит импланты, они не отзываются.
cyberware-blocked = Ошейник щёлкает, и все импланты разом затихают. Тело кажется непривычно тяжёлым.
cyberware-unblocked = Ошейник сняли, и импланты один за другим оживают.

ent-BulletEmpBolt = ЭМИ-заряд
    .desc = { "" }
ent-WeaponEmpPistol = ЭМИ-пистолет
    .desc = Табельное оружие СБ, стреляющее узконаправленными ЭМИ-зарядами. Первое попадание пробивает экранирование имплантов цели, а второе, если успеть быстро, ненадолго их отключает.
ent-ClothingNeckCyberwareBlocker = ошейник-блокиратор имплантов
    .desc = Тяжёлый ошейник, который забивает нервную систему помехами и глушит все импланты в теле. Снять его самостоятельно нельзя.

## Опознание и извлечение имплантов

cyberware-scanner-deep-started = Цель обездвижена, запускаете глубокое сканирование.
cyberware-scanner-deep-cooldown = Глубокое сканирование этого человека пока недоступно, сканер не успел откалибровать сигнатуры. Выполняется обычное.
cyberware-scanner-concealed = Неизвестных сигнатур: {$count}
cyberware-scanner-mantis-blade = клинок богомола ({$arm})

surgery-popup-implants-extracted = Извлечены и разрушены импланты: {$implants}.
surgery-popup-implants-nothing = Опознанных имплантов не найдено. Остальное без данных сканера не нащупать.

ent-SurgeryStepExtractImplants = Извлечь опознанные импланты
    .desc = { "" }
ent-SurgeryExtractImplants = Извлечение подкожных имплантов
    .desc = { ent-SurgeryBase.desc }
ent-SurgeryStepRemoveMantisBlade = Извлечь клинок богомола
    .desc = { "" }
ent-SurgeryRemoveMantisBlade = Извлечение клинка богомола
    .desc = { ent-SurgeryBase.desc }

## Имплант взлома

cyber-hack-cost = Импланты обжигают изнутри, взлом даётся телу недёшево.
cyber-hack-failed = Устройство не поддаётся взлому.

uplink-cyber-hack-implanter-name = Имплант взлома
uplink-cyber-hack-implanter-desc = Превращает вас в ходячий емаг: за две секунды взламывает и доступ, и скрытые функции устройства. Зарядов нет, но каждый взлом бьёт по здоровью и нагружает импланты. Сканер СБ видит его как неизвестную сигнатуру.

ent-ActionCyberHack = Взлом
    .desc = Протянуть к устройству цифровую связь и за две секунды взломать и его доступ, и скрытые функции. Каждый взлом обжигает тело и нагружает импланты.
ent-CyberHackImplant = имплант взлома
    .desc = Нелегальный киберимплант, который превращает носителя в ходячий емаг. Зарядов у него нет, но каждый взлом стоит здоровья.
ent-CyberHackImplanter = { ent-BaseImplantOnlyImplanterSyndi }
    .desc = { ent-BaseImplantOnlyImplanterSyndi.desc }
    .suffix = взлом

## Ингалятор нейроадаптина

uplink-neuroadaptin-inhaler-name = Ингалятор нейроадаптина
uplink-neuroadaptin-inhaler-desc = Многоразовый ингалятор, который сам понемногу синтезирует нейроадаптин. Помогает быстрее привыкать к новым имплантам и ставить их чаще, меньше рискуя рассудком. Ещё глушит симптомы киберпсихоза, пока нейроадаптин в крови, и вдвое снижает шанс, что он начнётся или станет тяжелее, до трёх минут после того, как нейроадаптин выведется. Один вдох восстанавливается примерно за минуту.

ent-NeuroadaptinInhaler = ингалятор нейроадаптина
    .desc = Компактный ингалятор, который сам понемногу синтезирует нейроадаптин. Один вдох помогает организму привыкнуть к недавно установленным имплантам, глушит симптомы киберпсихоза и снижает риск, что он станет тяжелее.
