"""Send absolute pointer events through the VirtualBox API."""

import json
import sys
import time


def fail(token):
    sys.stdout.write(token)
    raise SystemExit(1)


def require_vboxapi():
    try:
        from vboxapi import VirtualBoxManager
    except Exception:
        fail("api-unavailable")
    return VirtualBoxManager


def open_session(uuid):
    virtual_box_manager = require_vboxapi()
    manager = virtual_box_manager(None, None)
    virtual_box = manager.getVirtualBox()
    session = manager.getSessionObject(virtual_box)
    machine = virtual_box.findMachine(uuid)
    machine.lockMachine(session, manager.constants.LockType_Shared)
    return session


def close_session(session):
    try:
        session.unlockMachine()
    except Exception:
        return


def screen_count(session):
    try:
        count = int(session.machine.monitorCount)
    except Exception:
        fail("event-rejected")
    if count < 1 or count > 8:
        fail("event-rejected")
    return count


def read_screen(display, screen_id):
    try:
        width, height, _bits, x_origin, y_origin, status = display.getScreenResolution(
            screen_id)
    except Exception:
        fail("event-rejected")
    return {
        "display": screen_id,
        "width": int(width),
        "height": int(height),
        "x": int(x_origin),
        "y": int(y_origin),
        "active": int(status) == 1 and int(width) > 0 and int(height) > 0,
    }


def write_layout(uuid):
    session = open_session(uuid)
    try:
        console = session.console
        screens = [
            read_screen(console.display, index)
            for index in range(screen_count(session))
        ]
        absolute = bool(console.mouse.absoluteSupported)
        sys.stdout.write(json.dumps({"absolute": absolute, "screens": screens}))
    except SystemExit:
        raise
    except Exception:
        fail("event-rejected")
    finally:
        close_session(session)


def parse_events(values):
    if len(values) == 0 or len(values) % 6 != 0:
        fail("invalid-arguments")
    events = []
    for offset in range(0, len(values), 6):
        try:
            delay = int(values[offset])
            x = int(values[offset + 1])
            y = int(values[offset + 2])
            vertical = int(values[offset + 3])
            horizontal = int(values[offset + 4])
            buttons = int(values[offset + 5])
        except ValueError:
            fail("invalid-arguments")
        if delay < 0 or delay > 1000 or buttons < 0 or buttons > 7:
            fail("invalid-arguments")
        if x == -1 and y == -1:
            fail("invalid-arguments")
        if x == 2147483647 or y == 2147483647:
            fail("invalid-arguments")
        events.append((delay, x, y, vertical, horizontal, buttons))
    return events


def send_events(uuid, values):
    events = parse_events(values)
    session = open_session(uuid)
    try:
        mouse = session.console.mouse
        if not bool(mouse.absoluteSupported):
            fail("absolute-unavailable")
        for delay, x, y, vertical, horizontal, buttons in events:
            if delay:
                time.sleep(delay / 1000)
            mouse.putMouseEventAbsolute(x, y, vertical, horizontal, buttons)
    except SystemExit:
        raise
    except Exception:
        fail("event-rejected")
    finally:
        close_session(session)


def main(argv):
    if len(argv) < 3:
        fail("invalid-arguments")
    command = argv[1]
    uuid = argv[2]
    if command == "layout":
        write_layout(uuid)
        return
    if command == "events":
        send_events(uuid, argv[3:])
        return
    fail("invalid-arguments")


if __name__ == "__main__":
    try:
        main(sys.argv)
    except SystemExit:
        raise
    except Exception:
        fail("event-rejected")
