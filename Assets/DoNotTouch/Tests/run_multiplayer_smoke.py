"""Run one host, two early clients and one late client from the development macOS build.
Usage: python3 Assets/DoNotTouch/Tests/run_multiplayer_smoke.py
Only terminates processes launched by this script. Writes logs under TestResults/.
"""
from pathlib import Path
import plistlib
import subprocess
import time
import sys
import argparse

parser = argparse.ArgumentParser(description=__doc__)
scene = parser.add_mutually_exclusive_group()
scene.add_argument("--night01", action="store_true")
scene.add_argument("--horror", action="store_true")
parser.add_argument("--clients", type=int, choices=(1, 3), default=3)
parser.add_argument("--pass-name", default="", help="Optional result subdirectory, e.g. Cleanup")
args = parser.parse_args()

project = Path(__file__).resolve().parents[3]
app = project / ("Builds/macOS/DoNotTouch_Night01.app" if "--night01" in sys.argv else "Builds/macOS/DoNotTouch_Horror.app" if "--horror" in sys.argv else "Builds/macOS/DoNotTouch.app")
with (app / "Contents/Info.plist").open("rb") as stream:
    executable = app / "Contents/MacOS" / plistlib.load(stream)["CFBundleExecutable"]
logs = project / ("TestResults/Night01" if "--night01" in sys.argv else "TestResults/Horror" if "--horror" in sys.argv else "TestResults")
if args.pass_name:
    logs = logs / args.pass_name
logs = logs / f"host-plus-{args.clients}"
logs.mkdir(parents=True, exist_ok=True)
processes = []


def launch(name, role):
    log = logs / f"smoke-{name}.log"
    if log.exists():
        log.write_text("")
    process = subprocess.Popen([
        str(executable), "-batchmode", "-nographics", f"-dnt-smoke-{role}",
        *(["-dnt-smoke-two-players"] if args.clients == 1 else []),
        "-logFile", str(log),
    ], stdout=subprocess.DEVNULL, stderr=subprocess.STDOUT)
    processes.append((name, process, log))


try:
    launch("host", "host")
    time.sleep(2)
    for index in range(1, args.clients):
        launch(f"client{index}", "client")
    deadline = time.monotonic() + 65
    host_log = processes[0][2]
    while time.monotonic() < deadline:
        if host_log.exists() and "DNT_SMOKE_STAGED" in host_log.read_text(errors="replace"):
            break
        if processes[0][1].poll() is not None:
            raise RuntimeError("Host exited before staging; inspect TestResults/smoke-host.log")
        time.sleep(.25)
    else:
        raise TimeoutError("Host never staged the objectives")
    launch("late-client", "client")
    for name, process, log in processes:
        code = process.wait(timeout=85)
        contents = log.read_text(errors="replace")
        expected = "DNT_SMOKE_HOST_PASS" if name == "host" else "DNT_SMOKE_CLIENT_PASS"
        if code != 0 or expected not in contents:
            raise RuntimeError(f"{name}: exit={code}; inspect {log}")
        if args.night01 and name != "host":
            for marker in ("DNT_NIGHT01_SNAPSHOT_PASS", "DNT_HORROR_EVENT_PASS", "DNT_NIGHT01_RESTORE_PASS", "DNT_FLASHLIGHT_ON_SYNC_PASS", "DNT_FLASHLIGHT_OFF_SYNC_PASS"):
                if marker not in contents:
                    raise RuntimeError(f"{name}: missing {marker}; inspect {log}")
        print(f"PASS {name}", flush=True)
    print(f"PASS: host + {args.clients} clients, late join, state replication and shift completion", flush=True)
finally:
    for _, process, _ in processes:
        if process.poll() is None:
            process.terminate()
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()
