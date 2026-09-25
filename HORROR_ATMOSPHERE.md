# Horror atmosphere pass

## Scene and rollback

Primary scene: `Assets/DoNotTouch/Scenes/PrototypeMuseum_Horror.unity`.
Original: `Assets/DoNotTouch/Scenes/PrototypeMuseum.unity` (preserved).
Pre-pass source/assets backup: `Backups/BeforeHorror/DoNotTouch`.
Separate player prefab: `HorrorPlayer.prefab`; separate materials, volume and URP asset. The original player prefab and original scene remain intact.

Tools → Do Not Touch → Build/Rebuild Prototype now targets the horror scene. Build opens/validates an existing version; Rebuild regenerates presentation from the original museum. Legacy submenu opens/rebuilds the original prototype. Rebuilding either generated scene replaces manual edits in that generated scene.

## Visual design

- Six original rooms, original entrances and task destinations.
- Cool plaster/concrete, aged oak, timber, patinated metal/brass, translucent display glass, rough dark stone. Small deterministic procedural texture assets generated through Unity APIs.
- Warm Staff Room, isolated blue-grey Main Hall pools, gallery exhibit spotlights, sparse Storage light and red service emergency fixtures.
- Working, switched-off and event-controlled lamps. Emergency lamps and Staff Room remain on during outages.
- Ceilings, beams, stone door surrounds/arches, hall columns, benches, display framing, railings, wall panels, collection portraits and depth-tested signs.
- URP global Volume: moderate vignette/contrast/desaturation, fine grain, slight bloom, very subtle chromatic aberration and Neutral tonemapping.
- Exponential fog and pools of spotlight/shadow supply atmospheric depth. This is a lightweight approximation, not true volumetric ray marching.
- Local soft shadows, bounded 24m shadow distance, inherited PC renderer SSAO, 90% rendering scale. No new rendering packages or expensive ray tracing.

## Audio

`MuseumAmbience` synthesizes six small mono PCM clips once on spawn: room tone, ventilation, electrical hum, wood/metal creak, low distant impact and relay chatter. Three quiet loops; at most one ambient event source. Spatial events originate in rooms, with distance attenuation and a stone-corridor reverb filter. Staff Room reduces intrusive ambience. No external audio files, downloads or licenses are needed. These are designed placeholders, not a finished sound library.

## Network pacing

`HorrorDirector` owns the event serial, kind, room, start time, duration and tension stage on the server. Each client evaluates the same lamp curve against NGO ServerTime. Audio uses the same event timeline; late clients do not replay stale one-shots. Events cannot target Staff Room, and emergency fixtures do not participate in outages.

First event: after about 110 seconds. Later intervals: 45–85 seconds (longer early). Stage boundaries: 22%, 48%, 76% of the shift. Initial events are mostly small creaks/flickers; section outages arrive later; museum-wide brief outages are rare and retain emergency/base lighting.

Watcher activates at 115–170 seconds with a 42-second movement cooldown. `WatcherStaging` is optional and exists only in the horror scene. Authored destinations include a corridor end, columns, doors, an exhibit backdrop and hall corners. Minimum player distance decreases from 11m to 7m, 4m and 2.4m; a single relocation cannot approach by more than 4m. Candidate poses visible to a player or occupied by geometry are rejected. Subtle body orientation changes use the existing NetworkTransform. Restored power may request a move, but never overrides the observation check. Visible head or chest prevents movement.

## Preserved gameplay

The player controller, input, pickup physics, doors, interaction contract/RPCs, objectives, clock and session management are reused. New scene dressing keeps delivery routes and objective zones accessible. No combat, jump-scare attacks, inventory, matchmaking or new networking stack.

## Verification tools

- Existing PlayMode suite now runs against both original and horror scenes.
- Additional tests cover lamp restoration, protected base/emergency lighting, gradual tension, Watcher visibility during blackout and a head visible over a low exhibit.
- `python3 Assets/DoNotTouch/Tests/run_multiplayer_smoke.py --horror`: separate Host + three Client processes, late join, spawn/camera and gameplay state plus synchronized section blackout.
- `DoNotTouch.Editor.HorrorBuilder.BuildMac`: build `Builds/macOS/DoNotTouch_Horror.app`.
- Optional development flag `-dnt-atmosphere-review`: starts an isolated visual-review host. F1 Staff, F2 Hall, F3 Gallery, F4 Storage, F5 Corridor, F6 Security; F7 tests a Hall outage. It hides UI and locks movement only in this explicit review mode. Normal gameplay is unaffected.

## Run results

Final polish verified on 2026-09-24:

- Generated-scene validation and macOS development build passed without compilation errors.
- 17/17 EditMode and 19/19 PlayMode passed; gameplay suite covers both original and horror scenes. The added nineteenth test checks a visible Watcher head above an occluding exhibit.
- Final standalone Host + three Clients passed, including late join during section blackout, synchronized light state, protected Staff Room, player/camera spawn, gameplay replication and shift completion.
- Windowed standalone visual review confirmed the softened stone silhouette, smaller muted display plaques, warmer Staff Room, readable Storage objects and Hall blackout. All six rooms were reviewed during the pass; the three polished rooms were checked again in the final build.
- Rebuild now invokes NGO validation directly in the editor, avoiding Unity 6000.3 SendMessage assertions. This depends on the installed NGO editor validation method and fails clearly if that API changes.
- An initial final PlayMode attempt could not bind UDP 7777 because a diagnostic Host remained open. That process was closed and the full suite passed on rerun.
- Evidence: `TestResults/Horror/EditMode.xml`, `PlayMode.xml`, `PrototypeBuild.log`, `MacBuild.log`, `Visual.log`, and four `smoke-*.log` files.

Original scene, original Player prefab, player controller and session management remain byte-identical to the pre-pass backup. Audio initialization is covered by tests; subjective listening, four-human playfeel and WAN/network-loss testing remain future playtest work.
