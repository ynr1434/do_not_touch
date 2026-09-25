# DO NOT TOUCH — vertical slice

Unity 6000.3.24f1, URP, macOS; 1–4 players, NGO listen server.

1. Foundation: server-authoritative Rigidbody player, Input System, raycast interaction.
2. Physics: server-owned spring pickup, release/throw, replicated doors.
3. Shift: data-driven objectives, delivery zones, inspection, clock and completion.
4. Networking: connection approval, four-player cap, late join state, disconnect cleanup.
5. Watcher: server visibility with occlusion, timed movement between authored positions.
6. Editor builder: materials, prefab, six-room museum, networking, UI, validation.
7. Verification: compile, EditMode/PlayMode tests, generated-scene validation, multiplayer smoke test where available.
8. Documentation: controls, setup, testing instructions, remaining limitations.

All assets are generated through Unity Editor APIs. Existing template assets are preserved.

## Implementation status

Foundation, physics, objectives, clock, NGO state, Watcher, editor generator and documentation are implemented. Unity compilation and validation passed; 9 EditMode and 8 PlayMode tests passed. The macOS development build, four-process multiplayer smoke run (including late join, spawn/camera validation and completion), and windowed UI check passed. Full evidence and limits are in VALIDATION.md.

## Horror atmosphere pass

Separate PrototypeMuseum_Horror scene and pre-pass backup preserve the original prototype. Local lighting, muted procedural materials, architecture, URP Volume/fog, synthesized spatial ambience and server-authoritative events are implemented. Optional Watcher staging adds gradual approach and orientation changes. Final verification is tracked in HORROR_ATMOSPHERE.md.

## Night 01 expansion

The expansion is implemented as a separate generated scene and optional runtime layer. Eleven functional rooms provide roughly 3.5× the original usable area with central and side loops. `NightShiftDirector` adds seven server-owned stages without replacing legacy objective behavior. Scanner registry, power recovery, access gating, security snapshots, physical trolley, assisted heavy items, aggressive Watcher states/attack and physical team exit are wired by `Night01Builder`. Verification and known scope limits are documented in `NIGHT01.md` and `VALIDATION.md`.

Final verification on 2026-09-25: PlayMode 26/26, rebuilt standalone and scene validation passed. Separate Host + 1 and Host + 3 runs passed with late join during blackout, replicated Watcher state/anchor, objective phase, restored lights and Escape completion. Only the opt-in test harness and documentation changed in this verification pass; the fallback scene was not modified.

Geometry/flashlight verification on 2026-09-25: Night01 was regenerated on an aligned eleven-room modular grid with explicit wall openings, ceilings and door placement validation. A camera-mounted F-toggle flashlight is available from spawn and replicated through an authoritative NetworkVariable. EditMode 30/30, PlayMode 29/29, scene validation, macOS build, standalone visual review and Host + 1 late-join flashlight replication passed. The fallback scene was not modified.
