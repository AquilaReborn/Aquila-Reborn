orbital-strike-popup-launch = Orbital strike launched. Pods: { $count }.
orbital-strike-popup-busy = An orbital strike is already in progress.
orbital-strike-popup-count = Pod count: { $count }.
orbital-strike-popup-radius = Strike radius: { $radius }.
orbital-strike-popup-mode = Explosion mode: { $mode }.

orbital-strike-category-count = Pod count
orbital-strike-category-radius = Strike radius
orbital-strike-category-mode = Explosion mode

orbital-strike-verb-count = { $selected ->
    [true] [Selected] { $count }
   *[false] { $count }
}
orbital-strike-verb-radius = { $selected ->
    [true] [Selected] { $radius }
   *[false] { $radius }
}
orbital-strike-verb-mode = { $selected ->
    [true] [Selected] { $mode }
   *[false] { $mode }
}

orbital-strike-mode-weak = weak
orbital-strike-mode-medium = medium
orbital-strike-mode-strong = strong
