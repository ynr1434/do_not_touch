# Verification — DO NOT TOUCH

Environment: macOS / Apple Silicon, Unity 6000.3.24f1, NGO 2.7.0.

## Verified

- Actual Unity compilation of runtime, editor and test assemblies.
- Prototype scene, player prefab, materials and ShiftDefinition generated using Unity Editor APIs.
- Editor validation: NetworkManager/transport, player references, four spawns, objective zones, Watcher positions, exit, UI/EventSystem, missing scripts/references, unique nonzero NGO scene IDs.
- EditMode: **9 passed, 0 failed** — bounded objective progress, clock conversion, visibility angle/distance, Watcher movement rule.
- PlayMode: **8 passed, 0 failed** — real NGO host, keyboard/mouse movement/look/sprint/crouch/jump, pickup/release/throw, delivery filtering and departure, inspection/door/completion, Watcher observed/unobserved, session restart and host again, stable Staff Room spawn, interaction ray occlusion.
- NUnit XML: `TestResults/EditMode.xml`, `TestResults/PlayMode.xml` (generated output ignored by Git).

## Environment findings and fixes

- The filesystem sandbox prevented Unity Package Manager and Licensing IPC. Unity was run with approved access outside the sandbox.
- The first batch run left a blocked Licensing Client; that process was stopped and licensing recovered.
- An initial shutdown crashed in Burst JIT. Subsequent verification uses `-burst-disable-compilation`; no package was removed to work around it.
- NGO scene IDs must be generated after the first scene save. The builder now invokes NGO validation after saving, saves the generated IDs, and verifies uniqueness.
- Objective volumes read Rigidbody positions, avoiding renderer interpolation delay.
- Standalone exposed a spawn bug that did not reproduce in Editor tests: NGO assigns Transform after Instantiate, while the Rigidbody could retain the prefab origin. Connection approval supplies the spawn, and PlayerController explicitly initializes both Rigidbody and Transform before the first physics tick. Both PlayMode and standalone now check this regression.
- Input tests explicitly route keyboard/mouse events into PlayMode in batchmode and restore input settings afterward.

## Standalone verification — passed

- Development macOS app built successfully: `Builds/macOS/DoNotTouch.app`.
- Four independent standalone processes: one host, two early clients and one late client. All exited with code 0 and their expected PASS markers.
- Every process verified a stable Staff Room spawn and that the owner's camera matches its eye position before the scenario changed any coordinates.
- Host: body/root `(-12, 0.90, -7)`, camera/eye `(-12, 1.68, -7)`. Clients used separate authored spawn points.
- Clients verified replicated objectives (3 crates, 3 inspections, returned statue), clock, open doors, Watcher state, delivered object positions, kinematic remote rigidbodies, four players and final completion.
- Visual inspection in the windowed app: connection menu, Host button, readable room labels, enclosed Staff Room, attached first-person camera, HUD/clock/objectives, Esc menu, Leave shift and return to connection menu.
- Detailed logs: `TestResults/smoke-host.log`, `smoke-client1.log`, `smoke-client2.log`, `smoke-late-client.log`, `MacBuild.log`, `PrototypeBuild.log`, `Visual.log`.

The network scenario deliberately positions objective objects through server-side test code. It verifies replication and completion, not a manual four-human playthrough of every delivery route.

## Scope limits

Tests verify a prototype, not production networking under WAN latency or packet loss. Translation is server-authoritative without client prediction. Four-person subjective playfeel testing, Windows, Steam and long-duration network soak tests remain outside this local automated verification. The original scene retains debug TextMesh signage. The horror scene now uses depth-tested world labels.


## Final horror pass — 2026-09-24

Separate `PrototypeMuseum_Horror` and `DoNotTouch_Horror.app` verified after visual polish: 17/17 EditMode, 19/19 PlayMode, generated-scene validation and macOS build passed. Host + 3 Client standalone smoke passed with late join during blackout and spawn/camera regression checks. Windowed visual review covered all rooms during the pass and rechecked Hall/Staff/Storage in the final build. Details and evidence paths: `HORROR_ATMOSPHERE.md`. The baseline scene remains preserved.

## Night 01 expansion — 2026-09-24

`DoNotTouch_Night01` is a separate generated scene; `PrototypeMuseum_Horror` is preserved byte-for-byte in `Backups/BeforeNight01`. Automated validation passes for eleven rooms, navigation anchors, Electrical/fuses, scanner registry, continuous phase references, restricted access, five security cameras, trolley, heavy exhibit, Staff Exit and NetworkObject IDs.

New automated coverage exercises phase prerequisites, registry results, difficulty scaling, Watcher states, fuse rules, sequential unlock, blackout recovery, scanner, observed/unobserved movement, attack/drop/relocation/reset and exit gating. Final results: 29/29 EditMode and 26/26 PlayMode. The macOS development build succeeded. Standalone Host + three Clients passed with late join during Gallery B blackout and synchronized phase, power and Hunting state. Build, test, smoke and visual-review evidence is stored under `TestResults/Night01`.

Automated staging proves state replication and completion, not the requested human 10–25 minute playtime. Human solo/co-op pacing and subjective audio/navigation quality remain explicit playtest work.

## Final verification — 2026-09-25

- PlayMode: 26/26 passed, including existing scenes and Night 01. Evidence: `TestResults/Night01/PlayMode.xml` and `PlayMode.log`.
- Standalone rebuilt successfully after extending the opt-in smoke harness; scene and serialized reference validation passed. Evidence: `TestResults/Night01/MacBuild.log`.
- Host + 1 Client and Host + 3 Clients passed independently. The last client joined during the blackout in each run. Each client verified the RestrictedArea phase, fuse/power state, Hunting state and Watcher position matching its replicated anchor, protected Staff lighting, then restored Gallery B lights, Escape phase and completion. Evidence: `TestResults/Night01/host-plus-1/` and `host-plus-3/`.
- Previous EditMode result remains 29/29; no gameplay rules changed during this verification pass. Previous visual review is preserved in `Visual.log`; it was not repeated because no visual assets changed.
- `PrototypeMuseum_Horror.unity` SHA-256 matches `Backups/BeforeNight01/PrototypeMuseum_Horror.unity`. Diagnostic processes finished and released port 7777.

The harness deliberately stages server state and an Escape transition. These results validate replication and regression checks, not a full human playthrough or the claimed playtime targets.
