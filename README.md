# Crystal Code Computer

**A Crystal Code plugin that lets the agent see and control a VirtualBox VM.**

This is a plugin for [Crystal Code](https://github.com/YELANDAOKONG/CrystalCode). It drives one already-running VirtualBox VM: status and Guest Additions detection, a display capture, keyboard and pointer input, and one guest process. The host names the VM once; the model never chooses it.

[Features](#features) · [Tools](#tools) · [Configuration](#configuration) · [Build from source](#build-from-source)

## Features

- **Work with the screen.** `computer_observe` captures any display as an image, and click, drag, and scroll take pixel coordinates from that image.
- **Type and press keys.** `computer_type` sends tab, newline, and printable ASCII on a US keyboard; `computer_keys` presses single keys and shortcuts.
- **Run one guest process.** `computer_run` starts a program inside the VM with bounded arguments and a timeout. Guest credentials come from the process environment, never from the model.
- **Stay within one VM.** Every call uses the VM UUID from `CRYSTAL_COMPUTER_VM_UUID`. `VBoxManage` and `python3` are invoked without a shell.
- **Fit the approval model.** Reads and controls are classified separately, so the host approval mode decides when the VM tools may run.

## Tools

| Tool | Access | What it does |
| :--- | :--- | :--- |
| `computer_status` | Read | Report the VM state and whether Guest Additions are loaded |
| `computer_observe` | Read | Capture one display as an image; requires image input support |
| `computer_type` | Privileged | Type text at the current focus (1–4096 characters) |
| `computer_keys` | Privileged | Press a key or shortcut (1–4 keys) |
| `computer_click` | Privileged | Click at pixel coordinates; left, right, or middle, 1–2 clicks |
| `computer_drag` | Privileged | Drag from one pixel to another |
| `computer_scroll` | Privileged | Scroll the pointer wheel at pixel coordinates, 1–20 steps |
| `computer_run` | Privileged | Run one program inside the VM, up to 32 arguments |

Pointer coordinates are pixels from the top left of the `computer_observe` image, and the VM mouse must be an absolute device such as a USB Tablet. The plugin joins the Work catalog; `computer_status` and `computer_observe` are reads, and the other tools are classified as privileged.

## Configuration

Set `CRYSTAL_COMPUTER_VM_UUID` to the UUID of the VM the agent may control. The VM must already be running. Tested with VirtualBox 7.2.x.

| Variable | Required | Purpose |
| :--- | :--- | :--- |
| `CRYSTAL_COMPUTER_VM_UUID` | Yes | UUID of the VM every call targets |
| `CRYSTAL_COMPUTER_VBOXMANAGE` | No | Absolute path to `VBoxManage`; defaults to `VBoxManage` on `PATH` |
| `CRYSTAL_COMPUTER_VBOX_HOME` | No | VirtualBox installation directory when it is not in the usual location |
| `CRYSTAL_COMPUTER_PYTHON` | No | Python executable for the pointer API; defaults to `python3` on `PATH` |
| `CRYSTAL_COMPUTER_GUEST_USERNAME` | For `computer_run` | Guest account the process runs as |
| `CRYSTAL_COMPUTER_GUEST_PASSWORD_FILE` | For `computer_run` | Path to a password file; not shown to the model |

`computer_status` reports Guest Additions from the running guest. Run level 0 means they are not loaded, level 1 means the drivers are loaded, and level 2 or higher means the guest service is active; that status is available only while the VM is running. `computer_run` requires the active state. Guest paths are paths inside the VM.

Pointer tools call VirtualBox's pointer API through `python3` and the `vboxapi` bindings shipped with VirtualBox. `computer_observe` requires a model and provider that support image input. Before each model call, a hook drops screenshots from this plugin's older `computer_observe` results, so the three newest captures are still sent; the session keeps every screenshot, and images from any other tool stay on the request.

### Install

Publish `CrystalCode.Computer/CrystalCode.Computer.csproj` and place the published files together with `plugin.json` in one directory under `~/.crystal/plugins`. Then enable Plugins in Crystal Code.

## Build from source

You need the .NET 10 SDK, a sibling checkout of [Crystal](https://github.com/YELANDAOKONG/Crystal) at `../Crystal`, and a sibling checkout of [CrystalCode](https://github.com/YELANDAOKONG/CrystalCode) at `../CrystalCode`.

```bash
dotnet build CrystalCode.Computer.sln
dotnet test CrystalCode.Computer.sln
dotnet publish CrystalCode.Computer/CrystalCode.Computer.csproj
```

`Crystal.Tools` and `CrystalCode.Plugins` are referenced as sibling source projects, and this project adds no NuGet packages. `IComputerAdapter` keeps the tool definitions independent from VirtualBox; `VBoxManageAdapter` is the first implementation. It invokes `VBoxManage` without a shell and uses the configured VM UUID for every call, and pointer input follows the same rule for `python3`. This is in-process trusted code, not a host-enforced VM sandbox.
