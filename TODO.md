# TODO

## MVP
- [x] Реализованы системы vertical slice и генератор сцены.
- [x] Компиляция, Editor validation, 9 EditMode + 8 PlayMode тестов.
- [x] macOS build, Host + 3 Client, late join, spawn/camera и завершение смены.
- [x] Визуально проверены старт в Staff Room, HUD, пауза и возврат в меню.
- Результаты и границы проверок: VALIDATION.md.

## Horror atmosphere
- [x] Отдельная horror-сцена, резервная копия и автоматическая генерация.
- [x] Локальный свет, материалы, архитектура, Volume и пространственный ambience.
- [x] Серверные light events, позднее подключение во время blackout и постепенная постановка Watcher.
- [x] Финальный polish силуэта, табличек и света Staff Room/Storage.
- [x] Финальная macOS-сборка, 17/17 EditMode, 19/19 PlayMode и Host + 3 Client с late join во время blackout.

## NEXT
- [x] Night 01: 11-room map, loops, alternate routes and Staff Exit.
- [x] Sequential server-authoritative phase chain and hidden/timed objectives.
- [x] Scanner registry, power/fuse sequence, Security utility and restricted access.
- [x] Watcher state machine, cooperative observation rule and non-lethal attack/reset.
- [x] Physical trolley, solo-capable heavy objects and nearby-player assistance.
- [x] Night 01 validation, 29/29 EditMode, 26/26 PlayMode, macOS build and Host + 3 Client late-join smoke.
- [x] 2026-09-25: cleanup pass — PlayMode 28/28, EditMode 30/30, финальная standalone-сборка, solo Watcher proof (activation/move/freeze/resume), Host + 1 и Host + 3 с late join, реальный Watcher step, blackout и восстановление питания; логи сохранены в TestResults/Night01/Cleanup.
- [x] Onboarding clarity pass: Staff Room shift board, gated first objective, active destination waypoint, scanner onboarding, QA reset and Watcher debug toggle rebuilt and covered by final PlayMode suite.
- [x] Geometry cleanup + flashlight hard fix: modular aligned room grid/openings, corrected Service Corridor sign, local F-controlled light, replicated remote state, validation, macOS build and Host + 1 late-join smoke. Evidence: `TestResults/Night01/GeometryFlashlight*`.
- [x] Objective guidance pass: exact A-01…B-03 crate/slot matching, room-entry waypoint, held-item highlight, placement feedback/chime, sequential crate copy and Tab history. Validation, 32/32 PlayMode, macOS build and standalone visual review passed.
- [x] AR-01 progression soft-lock: moved its placement from the phase-locked Electrical Room into reachable Archaeology, added objective access dependencies and marker/delivery guards, audited every Night01 objective, passed 34/34 PlayMode and macOS standalone build. Evidence: `TestResults/Night01/SoftLock*`.
- Human pacing pass: verify 12–15 minute experienced solo and 10–18 minute coordinated co-op targets.
- Four-human playtest of Watcher readability, heavy carry and navigation under pressure.
- Кооперативный playtest четырьмя людьми: удобство переноса, обзорность, темп смены.
- [x] Horror-сцена использует таблички с проверкой глубины.
- Авторские звуковые записи, более разнообразные коллекционные экспонаты и настройка темпа по playtest.
- Client prediction/reconciliation для более высокой сетевой задержки.
- Настройка массы, пружины и дверных проёмов по результатам кооперативного playtest.
- Улучшить анимацию приседания удалённого игрока, звук и обратную связь о повреждении.
- Проверки при потере пакетов, переподключении и разных соотношениях сторон окна.

## LATER
- Windows / Steam, matchmaking и voice chat.
- Новые аномалии и задания, художественные ассеты, звук, сохранения.
