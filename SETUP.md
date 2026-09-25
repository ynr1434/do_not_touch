# Setup

1. Открыть проект в Unity **6000.3.24f1** через Unity Hub; дождаться импорта пакетов.
2. **Tools > Do Not Touch > Open Night 01**, затем **Play → Host shift**.

Все ссылки и ресурсы уже созданы генератором. Для локального клиента запустить вторую собранную копию и выбрать **Join host**, IP `127.0.0.1`.

По умолчанию открывается `Assets/DoNotTouch/Scenes/DoNotTouch_Night01.unity`.
Готовая macOS-сборка: `Builds/macOS/DoNotTouch_Night01.app`.
Освещение, материалы, Volume и ambience уже настроены; вручную расставлять их не нужно.
Фонарик переключается клавишей **F**; сканер берётся и используется клавишей **E**.

Перегенерация Night 01: **Tools > Do Not Touch > Rebuild Night 01**. Она заменяет ручные правки Night 01. Fallback `PrototypeMuseum_Horror` сохранён, его резервная копия — `Backups/BeforeNight01`.

Сборка из batchmode: `-executeMethod DoNotTouch.Editor.Night01Builder.BuildMac`.
Локальная сетевая проверка: `python3 Assets/DoNotTouch/Tests/run_multiplayer_smoke.py --night01`.
Для Host + 1 Client добавьте `--clients 1`; для Host + 3 Clients — `--clients 3` (по умолчанию). В обоих случаях последний клиент подключается во время blackout. Логи: `TestResults/Night01/host-plus-1` и `host-plus-3`.
Проверка также требует маркеры `DNT_FLASHLIGHT_ON_SYNC_PASS` и `DNT_FLASHLIGHT_OFF_SYNC_PASS`: удалённый игрок видит оба сетевых состояния и соответствующее состояние `Light`.
Перед проверкой закройте остальные игровые Host-процессы (UDP 7777).
Подробности атмосферы и диагностического режима: `HORROR_ATMOSPHERE.md`.
Для финальной проверки Watcher: `Builds/macOS/DoNotTouch_Night01.app/Contents/MacOS/do_not_touch -batchmode -nographics -dnt-watcher-solo -dnt-watcher-auto -logFile TestResults/Night01/CleanupSoloWatcher.log`. Успех отмечается `WATCHER_SOLO_PASS`.
