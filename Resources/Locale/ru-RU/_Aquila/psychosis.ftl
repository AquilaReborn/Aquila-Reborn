## Раздвоение личности

split-personality-alter-awakened = В голове будто кто-то проснулся. Теперь вы не одни в этом теле.
split-personality-swap-no-alter = Вторая личность ещё не проснулась.
split-personality-swap-suppressed = Лекарство глушит вторую личность, сейчас до неё не дотянуться.
split-personality-swap-pending = Вы уже ждёте ответа от второй личности.
split-personality-swap-unavailable = Вторая личность не откликается.
split-personality-swap-requested = Вы тянетесь к другой своей личности и ждёте ответа...
split-personality-swap-denied = Другая личность не захотела меняться местами.
split-personality-swap-surfaced = Вы выходите на поверхность и снова чувствуете тело.
split-personality-swap-receded = Вы уходите вглубь сознания и смотрите на мир со стороны.
split-personality-faded = Вторая личность затихает и пропадает.
split-personality-cured = Мысли проясняются: в голове снова только вы.

split-personality-request-title = Смена личности
split-personality-request-text = Другая ваша личность просит поменяться местами. Если вы согласитесь, управление телом перейдёт к той личности, что сейчас внутри, а вторая будет наблюдать изнутри. Внутренняя связь между личностями через [bold]+з[/bold].
split-personality-request-accept = Поменяться
split-personality-request-deny = Отказаться

collective-mind-split-personality-inner-voice = Внутренний голос

ghost-role-information-split-personality-name = Вторая личность
ghost-role-information-split-personality-description = У одного из членов экипажа раздвоение личности, и вы его вторая личность. У вас своё имя, характер и взгляды, но тело общее. Меняться местами можно только по взаимному согласию.
ghost-role-information-split-personality-rules = Вы не антагонист. Вы делите тело с другим игроком: уважайте его, общайтесь через внутренний голос и не используйте тело во вред ему и экипажу. Галоперидол глушит вас, а нейролептин может вылечить раздвоение совсем.

## Воображаемый друг

imaginary-friend-appeared = Вам кажется, что рядом кто-то есть.
imaginary-friend-cured = Вы больше не чувствуете рядом того, кто всегда был с вами.

ghost-role-information-imaginary-friend-name = Воображаемый друг
ghost-role-information-imaginary-friend-description = Вы воображаемый друг одного из членов экипажа. Вас видит и слышит только он, остальные для вас как будто не существуют, а вы для них.
ghost-role-information-imaginary-friend-rules = Вы не антагонист. Вы не можете ни к чему прикасаться и всегда держитесь рядом со своим другом. Галоперидол заставляет вас исчезнуть, пока лекарство в крови, а нейролептин может избавить от вас насовсем.

## Сущности

ent-ActionSplitPersonalitySwap = Сменить личность
    .desc = Попросить другую свою личность поменяться местами.
ent-SplitPersonalityPassenger = внутренняя личность
    .desc = { "" }
ent-SplitPersonalityAlter = { ent-SplitPersonalityPassenger }
    .desc = { ent-SplitPersonalityPassenger.desc }
ent-ImaginaryFriend = воображаемый друг
    .desc = Кто-то, кто точно здесь есть. Наверное.

## Лекарство

reagent-name-neuroleptin = нейролептин
reagent-desc-neuroleptin = Сильный антипсихотик. Если продержать его в крови около минуты, избавляет от раздвоения личности и воображаемых друзей. Вызывает сонливость, при передозировке токсичен.

## Событие и админские действия

station-event-psychic-anomaly-announcement = Внимание экипажу: датчики станции зафиксировали всплеск психической активности, у некоторых членов экипажа возможны нарушения восприятия и раздвоение личности. Медицинскому отделу рекомендуется обследовать персонал, при выявлении симптомов назначать галоперидол или нейролептин.

psychosis-admin-verb-split = Выдать раздвоение личности
psychosis-admin-verb-friend = Выдать воображаемого друга
psychosis-admin-verb-cure = Вылечить психоз
