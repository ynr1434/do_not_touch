# DO NOT TOUCH — Night 01

Verification status (2026-09-25): final Watcher cleanup pass is complete. The standalone solo harness proves the normal server timer: activation after 10 seconds, movement while unobserved, freeze while observed, and resumed movement after looking away. Evidence is in `TestResults/Night01/CleanupSoloWatcher.log`.

Onboarding clarity pass (source pending scene rebuild): the first objective is gated by the Staff Room shift board, future objectives stay hidden until progression, active zones expose a small room/distance waypoint, scanner pickup gives one-time instructions, and `-dnt-qa-menu` exposes the reset action for first-time-player review. Rebuild Night 01 before launching the updated onboarding in standalone.

Primary scene: `Assets/DoNotTouch/Scenes/DoNotTouch_Night01.unity`  
Fallback horror scene: `Assets/DoNotTouch/Scenes/PrototypeMuseum_Horror.unity`  
Pre-expansion snapshot: `Backups/BeforeNight01`

## Map

Night 01 contains eleven named gameplay rooms plus the Staff Exit: Staff Room, Loading/Receiving, Service Corridor, Security, Storage, Electrical, Main Hall, Gallery A, Gallery B, Ancient Wing and Archive/Restricted. Their combined usable room area is about 2,140 m² versus roughly 600 m² in the six-room prototype. Central, west and east connections provide alternate routes and loops. Room plaques, low-key direction boards, floor materials and local light colors identify each wing.

## Shift flow

The server owns the current phase and all critical state. The HUD reveals only the current work, not phase names.

1. Receive 4/5/6 crates according to player count, put three catalogued exhibits in matching zones, and inspect five cases.
2. Synchronized display anomalies appear, followed by a short quiet interval.
3. Gallery B loses power. Retrieve a fuse, insert it, then reset the Electrical Room breaker.
4. Watcher activates. Return the sculpture, review Security feeds and locate the unregistered exhibit with the scanner.
5. Archive opens. Retrieve the keycard, scan and return the restricted artifact.
6. Return two final objects, secure the case and verify the final collection seal.
7. Every connected player must physically reach Staff Exit.

The shift limit is 20 minutes. Task distance, physical transport and the gated sequence prevent immediate completion; actual 12–25 minute pacing remains subject to human playtesting.

## Tools and threats

- The handheld scanner is collected in Staff Room. Scannable exhibits use `Night01Registry.asset` and report registered/not-found IDs in the existing interaction UI.
- Three fuse locations feed one socket and breaker sequence. Power, phase, doors and blackout light state are NetworkVariables.
- Security uses five fixed camera nodes and a lightweight console readout instead of five continuous RenderTextures.
- The trolley is a server-owned rigidbody. Heavy exhibits remain solo-portable at reduced speed; a nearby second player increases carrying force.
- Watcher states are Dormant, Awake, Stalking, Hunting, CloseThreat and Disabled. The server chooses reachable authored navigation anchors with wall-clearance checks. Any active player looking at it freezes it. An unseen close approach drops the held item, blacks out the victim for 2.5 seconds, relocates them to a recovery point and resets the Watcher.
- Proximity is communicated through radio/electrical noise, stone scraping and final alarm ambience. There is no threat meter or instant death.

## Editor automation

`Tools > Do Not Touch` provides Build Night 01, Rebuild Night 01, Open Night 01 and Run Night 01 Validation. Rebuild generates rooms, routes, lights, signage, registry, objectives, scanner, power sequence, Watcher anchors, cameras, trolley, pickups, doors, UI references and network IDs. No Inspector wiring is required.

Batch build method: `DoNotTouch.Editor.Night01Builder.BuildMac`. Multiplayer smoke: `python3 Assets/DoNotTouch/Tests/run_multiplayer_smoke.py --night01`.

## Scope limits

Security feeds are intentional status snapshots rather than continuous video. Heavy-object cooperation uses nearby assistance rather than a fragile two-player joint. Procedural audio and geometry are designed placeholders. Human pacing, route clarity under pressure, packet-loss behavior and subjective audio balance still require playtests.
