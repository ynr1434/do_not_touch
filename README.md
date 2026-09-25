# DO NOT TOUCH

**The museum is closed. The exhibits are not.**

Кооперативный physics comedy / light horror прототип для 1–4 сотрудников ночного музея. Unity **6000.3.24f1 (6.3 LTS)**, URP, C#, macOS. Netcode for GameObjects **2.7.0** + Unity Transport, новый Input System.

## Запуск

Готовая macOS development-сборка первой полной смены: `Builds/macOS/DoNotTouch_Night01.app`. Можно запустить её напрямую и нажать **Host shift**. Предыдущая проверенная версия сохранена как `DoNotTouch_Horror.app`.

Откройте этот каталог через Unity Hub. Откройте `Assets/DoNotTouch/Scenes/DoNotTouch_Night01.unity` (или **Tools > Do Not Touch > Open Night 01**). Нажмите Play → **Host shift**.

Night 01 длится до 20 реальных минут. Задания выдаются последовательно: приём и расстановка коллекции, первые аномалии, восстановление Gallery B, работа со сканером и Security, Archive, финальная проверка и физический выход всей команды через Staff Exit. HUD показывает только текущую задачу, а **Tab** — только завершённые пункты и текущую цель. У ящиков A-01…B-03 есть точно подписанные площадки; правильная рамка подсвечивается, пока ящик в руках. Сканер берётся и используется клавишей **E**.

Watcher активируется после восстановления питания и движется по достижимым музейным anchors только когда его не видит ни один игрок. При близком нападении он выбивает переносимый предмет, на 2.5 секунды гасит экран и переносит сотрудника в другую служебную точку. В кооперативе один игрок может удерживать статую взглядом, пока другой выполняет interaction. Для ручной проверки solo движения используется development-флаг `-dnt-watcher-solo -dnt-watcher-auto`; он выдаёт `WATCHER_SOLO_PASS` после остановки под взглядом и возобновления.

## Управление

| Клавиша | Действие |
|---|---|
| WASD / мышь | Движение / обзор |
| Shift / Space | Бег / прыжок |
| Ctrl или C (удерживать) | Приседание |
| E | Взаимодействие / взять / отпустить |
| LMB | Взять / использовать / отпустить |
| RMB | Бросить удерживаемый предмет |
| F | Включить / выключить фонарик |
| Tab | Показать / скрыть задания |
| Esc | Меню; сетевая смена продолжает идти |

## Multiplayer

Один процесс: **Host shift**. Другой процесс: IP хоста → **Join host**. На одном Mac используйте `127.0.0.1`; в LAN — адрес компьютера хоста. UDP порт **7777**, максимум четыре игрока. Интернет/Relay/Steam пока не входят в прототип.

Для теста Editor + standalone используйте DoNotTouch_Night01 первой сценой или вызовите `DoNotTouch.Editor.Night01Builder.BuildMac` через batchmode. В обеих копиях должна быть одна версия сцены/проекта. Кнопка New session/Leave shift перезапускает локальную сцену; выход хоста отключает клиентов.

Сервер рассчитывает движение, физику, захват и броски, проверяет дистанцию взаимодействий, считает задачи/время, переключает двери и Watcher. Клиенты отправляют ввод; состояние позднего подключения приходит через NetworkVariables/NetworkTransform. Предметы остаются собственностью сервера; Holder блокирует одновременный захват. Перемещение клиента без prediction рассчитано прежде всего на локальное тестирование.

## Editor tooling

- **Build Prototype** — создаёт сцену и ресурсы; если сцена существует, открывает и проверяет её.
- **Rebuild Prototype** — пересоздаёт horror-сцену, HorrorPlayer и её материалы/свет/Volume. Ручные правки этих ресурсов заменяются; исходный prototype и NightShift сохраняются.
- **Run Validation** — проверяет сеть, игрока, ссылки, четыре spawn point, задания, Watcher, выход, EventSystem и UI.
- **Open Prototype Scene** — открывает готовую сцену.
- **Build/Rebuild Night 01** — создаёт или полностью перегенерирует расширенную смену.
- **Open Night 01** — открывает основную сцену.
- **Run Night 01 Validation** — проверяет карту, фазы, scanner, power, Watcher, камеры, trolley, heavy object и Exit.

Исходные template assets не удалены. Сгенерированные `.unity`, `.prefab`, `.mat`, `.asset` созданы официальными Editor APIs. Настройки смены находятся в `Assets/DoNotTouch/ScriptableObjects/NightShift.asset`.

## Тесты

Window → General → Test Runner: EditMode и PlayMode. PlayMode использует сгенерированную сцену и настоящий NGO host. Проверяются захват/бросок, доставка, осмотр, двери, завершение смены и Watcher. Сводка фактических запусков — `VALIDATION.md`.

Пример CLI (путь соответствует установленному редактору):

```sh
/Applications/Unity/Hub/Editor/6000.3.24f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -burst-disable-compilation \
  -projectPath "$PWD" -runTests -testPlatform EditMode \
  -testResults /tmp/dnt-editmode.xml -logFile /tmp/dnt-editmode.log
```

Для PlayMode замените `EditMode` на `PlayMode`. Не открывайте один проект одновременно в Editor и batchmode. `-quit` не добавляйте к `-runTests`. Отключение Burst здесь обходит обнаруженный сбой завершения batchmode на этой системе; runtime-код прототипа от Burst не зависит.

После development build автоматическая проверка четырьмя процессами:

```sh
python3 Assets/DoNotTouch/Tests/run_multiplayer_smoke.py --night01
```

Она проверяет стартовые координаты игроков и камер, запускает Host + два Client, меняет серверные состояния, подключает позднего третьего Client и проверяет репликацию задач, физических объектов, дверей, часов, фонарика, Watcher и завершения смены. Логи сохраняются в `TestResults/`. Флаги `-dnt-smoke-host` / `-dnt-smoke-client` работают только в development build; без них сценарий проверки не запускается.

Для проверки Host + 1 Client добавьте `--clients 1`; для Host + 3 Clients — `--clients 3` (по умолчанию). Последний клиент подключается во время blackout. Проверяются фаза, состояние и позиция Watcher, питание, восстановление ламп и завершение смены; логи разделены в `TestResults/Night01/host-plus-1` и `host-plus-3`. Это проверка репликации с серверной постановкой сценария, а не замер полного прохождения.

Архитектура расширения описана в `NIGHT01.md`, предыдущий atmosphere pass — в `HORROR_ATMOSPHERE.md`. Исходные сцены и сборки сохранены.
