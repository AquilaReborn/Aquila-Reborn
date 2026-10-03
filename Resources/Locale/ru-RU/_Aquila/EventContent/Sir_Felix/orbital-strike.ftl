orbital-strike-popup-launch = Орбитальный удар запущен. Капсул: { $count }.
orbital-strike-popup-busy = Орбитальный удар уже идёт.
orbital-strike-popup-count = Количество капсул: { $count }.
orbital-strike-popup-radius = Радиус удара: { $radius }.
orbital-strike-popup-mode = Режим взрыва: { $mode }.

orbital-strike-category-count = Количество капсул
orbital-strike-category-radius = Радиус удара
orbital-strike-category-mode = Режим взрыва

orbital-strike-verb-count = { $selected ->
    [true] [Выбрано] { $count }
   *[false] { $count }
}
orbital-strike-verb-radius = { $selected ->
    [true] [Выбрано] { $radius }
   *[false] { $radius }
}
orbital-strike-verb-mode = { $selected ->
    [true] [Выбрано] { $mode }
   *[false] { $mode }
}

orbital-strike-mode-weak = слабый
orbital-strike-mode-medium = средний
orbital-strike-mode-strong = сильный
ent-OrbitalStrike = орбитальный удар
    .desc = Устройство для вызова орбитального удара. При активации обрушивает капсулы вокруг пользователя.
