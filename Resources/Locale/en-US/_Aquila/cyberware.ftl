## Cyberware scanner

cyberware-scanner-being-scanned = {CAPITALIZE(THE($user))} runs a scanner over you.
cyberware-scanner-adapting = The body is still adapting to recently installed implants.

cyberware-scanner-ui-title = Cyberware scanner
cyberware-scanner-ui-empty = Run the scanner over someone to see the results.
cyberware-scanner-ui-scan-quick = Quick scan
cyberware-scanner-ui-scan-deep = Deep scan
cyberware-scanner-ui-load = Cyber strain
cyberware-scanner-ui-percent = {$percent}%
cyberware-scanner-ui-count = Implants:
cyberware-scanner-ui-level = Strain:
cyberware-scanner-ui-implants = Implants
cyberware-scanner-ui-no-implants = No subdermal implants identified.
cyberware-scanner-ui-psychosis = Cyberpsychosis
cyberware-scanner-ui-stage = Stage: {$stage}
cyberware-scanner-ui-cure = Treatment: {$percent}%

cyberware-scanner-level-none = none
cyberware-scanner-level-low = low
cyberware-scanner-level-elevated = elevated
cyberware-scanner-level-high = high
cyberware-scanner-level-critical = critical

## Neuroadaptin

reagent-name-neuroadaptin = neuroadaptin
reagent-desc-neuroadaptin = Helps the nervous system get used to freshly installed implants much faster. Dulls cyberpsychosis symptoms and lowers the risk of it getting worse for a while. Toxic in overdose.

entity-effect-guidebook-adjust-cyber-strain =
    { $chance ->
        [1] { $deltasign ->
                [1] Increases
                *[-1] Reduces
            }
        *[other]
            { $deltasign ->
                [1] increase
                *[-1] reduce
            }
    } the strain from freshly installed implants


## Cyberpsychosis

cyberpsychosis-instability-enter = Something is wrong. The hardware inside you feels like it has a mind of its own, and static runs along the edges of your vision.
cyberpsychosis-instability-exit = The static fades away, your body has finally got used to the implants.
cyberpsychosis-instability-message-1 = For a split second the world in front of you falls apart into pixels.
cyberpsychosis-instability-message-2 = Something hums quietly under your skin, and the hum just won't go away.
cyberpsychosis-instability-message-3 = Your fingers stop obeying for a moment, as if the signal never reached them.
cyberpsychosis-instability-message-4 = Something clicks in your ears like a bad speaker.
cyberpsychosis-instability-message-5 = It feels like your implants work a little faster than you can think.
cyberpsychosis-instability-tremor = Your hands start to shake and you cannot make it stop.

cyberpsychosis-rejection-enter = Your body is rejecting the hardware. You taste metal, and your head buzzes so loudly you can barely hear your own thoughts.
cyberpsychosis-rejection-exit = The rejection recedes, but static still runs along the edges of your vision.
cyberpsychosis-rejection-cough = You break into a coughing fit and taste blood on your lips.
cyberpsychosis-rejection-spasm = Your body seizes up, the implants shock you from the inside!
cyberpsychosis-rejection-message-1 = A voice speaks right next to you, but there is nobody there.
cyberpsychosis-rejection-message-2 = The metal under your skin burns as if it was red hot.
cyberpsychosis-rejection-message-3 = You do not remember how you got here.
cyberpsychosis-rejection-message-4 = Your thoughts tangle up, drowned out by someone else's digital noise.
cyberpsychosis-rejection-message-5 = Your own reflection looks strange to you, far too mechanical.

entity-effect-guidebook-bleed-out =
    { $chance ->
        [1] Spills
        *[other] spill
    } {$amount}u of blood out of the body

cyberpsychosis-breakdown-enter = Something inside you snaps. The world turns red, and the people around you no longer look like people. You are a cyberpsycho: the hardware is in control, and the crew and security are free to stop you by any means.
cyberpsychosis-breakdown-examine = [color=red]{CAPITALIZE(SUBJECT($target))} stares straight through you with empty, unblinking eyes, and the implants under {POSS-ADJ($target)} skin flicker and spark. Something is very wrong with {OBJECT($target)}.[/color]
cyberpsychosis-breakdown-rampage = Your body no longer obeys you. Your hands reach for a weapon on their own!

cyberpsychosis-breakdown-warcry-1 = You are all... you are all the same! Stay back!
cyberpsychosis-breakdown-warcry-2 = I can see what you really are!
cyberpsychosis-breakdown-warcry-3 = Target acquired. Eliminate.
cyberpsychosis-breakdown-warcry-4 = Stop it! Stop looking at me!

cyberpsychosis-breakdown-paranoia-1 = They are watching me. All of them.
cyberpsychosis-breakdown-paranoia-2 = That is not your face. Take it off!
cyberpsychosis-breakdown-paranoia-3 = Who sent you?
cyberpsychosis-breakdown-paranoia-4 = I can hear you whispering behind my back.

cyberpsychosis-breakdown-machine-1 = Error... error... rebooting...
cyberpsychosis-breakdown-machine-2 = Flesh is weak. Steel is forever.
cyberpsychosis-breakdown-machine-3 = I cannot feel my hands anymore. Only the signal.
cyberpsychosis-breakdown-machine-4 = Noise... so much noise in my head...

## Cyberpsychosis treatment

cyberpsychosis-stage-instability = instability
cyberpsychosis-stage-rejection = rejection
cyberpsychosis-stage-breakdown = breakdown

cyberpsychosis-suppressed = The noise in your head fades, as if someone turned the volume down. You can think again.
cyberpsychosis-suppression-ended = The medicine wears off and the noise in your head comes back.
cyberpsychosis-therapy-blocked = The medicine cannot cope: there is too much hardware in your body.
cyberpsychosis-cured = The static fades out bit by bit. You feel like yourself again, and the implants are just part of your body.

reagent-name-cybersynaptin = cybersynaptin
reagent-desc-cybersynaptin = Restores the link between the nervous system and implants. Kept in the blood for a couple of minutes without a break, it cures cyberpsychosis, but only while the cyber strain is below critical. Causes drowsiness and is toxic in overdose.

entity-effect-guidebook-suppress-cyberpsychosis =
    { $chance ->
        [1] Suppresses
        *[other] suppress
    } cyberpsychosis episodes
entity-effect-guidebook-cyberpsychosis-resistance =
    { $chance ->
        [1] Lowers
        *[other] lower
    } the chance of cyberpsychosis starting or getting worse by {$percent}% for {NATURALFIXED($time, 0)} {MANY("second", $time)}
entity-effect-guidebook-cyberpsychosis-therapy =
    { $chance ->
        [1] Treats
        *[other] treat
    } cyberpsychosis while the cyber strain is below critical

cyberpsychosis-admin-verb-cure = Cure cyberpsychosis

## Second arm slot and EMP

surgery-popup-augment-duplicate = This arm already has the same augment.

cyberware-action-disabled = The implant does not respond: an EMP knocked it out.
cyberware-emp-shield-broken = Your implants glitch and spark, their shielding is broken. The next pulse will shut them down!
cyberware-emp-disabled = Your implants shut down and a painful shock runs through your body!

## Security tools against implants

cyberware-action-blocked = The collar jams your implants, they do not respond.
cyberware-blocked = The collar clicks and all your implants go quiet at once. Your body feels unusually heavy.
cyberware-unblocked = The collar comes off and your implants come back to life one by one.

## Implant identification and extraction

cyberware-scanner-deep-started = The target is immobilized, starting a deep scan.
cyberware-scanner-deep-cooldown = A deep scan of this person is not available yet, the scanner has not recalibrated. Running a normal scan.
cyberware-scanner-concealed = Unknown signatures: {$count}
cyberware-scanner-mantis-blade = mantis blade ({$arm})

surgery-popup-implants-extracted = Extracted and destroyed implants: {$implants}.
surgery-popup-implants-nothing = No identified implants found. Without scanner data the rest cannot be located.

## Hacking implant

cyber-hack-cost = Your implants burn from the inside, the hack does not come cheap for your body.
cyber-hack-failed = The device resists the hack.

uplink-cyber-hack-implanter-name = Hacking implant
uplink-cyber-hack-implanter-desc = Turns you into a walking emag: in two seconds it breaks both the access and the hidden functions of a device. It has no charges, but every hack hurts you and strains your implants. Security scanners see it as an unknown signature.

## Neuroadaptin inhaler

uplink-neuroadaptin-inhaler-name = Neuroadaptin inhaler
uplink-neuroadaptin-inhaler-desc = A reusable inhaler that slowly synthesizes neuroadaptin on its own. Helps your body adapt to new implants faster, so you can install them closer together with less risk to your mind. It also briefly dulls cyberpsychosis symptoms and halves the chance of it starting or getting worse for three minutes after a puff. One puff recharges in about a minute.
