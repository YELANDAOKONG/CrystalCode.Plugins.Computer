# CrystalCode Computer

This is an independent native dotnet tool set for Crystal Code. It controls
one already-running VirtualBox VM: status and Guest Additions detection,
a display capture, text and key input, pointer click, drag, and scroll, and
one guest process.

Set `CRYSTAL_COMPUTER_VM_UUID` to the UUID of the VM the agent may control.
Optionally set `CRYSTAL_COMPUTER_VBOXMANAGE` to the absolute path of `VBoxManage`.
The VM must already be running. The tool set never accepts a VM identifier from
the model.

`computer_status` reports Guest Additions from the running guest.
Run level 0 means they are not loaded, level 1 means the drivers are loaded,
and level 2 or higher means the guest service is active. That status is
available only while the VM is running. `computer_run` requires the active
state, plus `CRYSTAL_COMPUTER_GUEST_USERNAME` and
`CRYSTAL_COMPUTER_GUEST_PASSWORD_FILE`. The password file is not shown to
the model. Guest paths are paths inside the VM.

Pointer tools use pixels from the top left of the `computer_observe` image.
The VM mouse must be an absolute device, such as a USB Tablet. Those tools
call VirtualBox's pointer API through `python3` and the `vboxapi` bindings
shipped with VirtualBox. Set `CRYSTAL_COMPUTER_VBOX_HOME` when VirtualBox is
not in the usual installation directory, and `CRYSTAL_COMPUTER_PYTHON` when
`python3` is not on `PATH`.

`computer_type` sends tab, newline, and printable ASCII on a US keyboard.
Other characters are rejected. Shortcuts use `computer_keys`.

Publish `CrystalCode.Computer/CrystalCode.Computer.csproj` and place the published files
and `tools.json` together in one directory under `~/.crystal/tools`. Enable
External Tools in Crystal Code and select a model and provider that support
image input for `computer_observe`. The manifest enables the Work catalog and
declares `approval: always`. That declaration takes effect when Crystal Code's
Home external tool approval source is `author`.

The .NET tool contracts are referenced as sibling source projects. This project
does not add NuGet packages. `IComputerAdapter` keeps the tool definitions
independent from VirtualBox. `VBoxManageAdapter` is the first implementation;
it invokes `VBoxManage` without a shell and uses the configured VM UUID for
every call. Pointer input uses the same rule for `python3`. This is in-process
trusted code, not a host-enforced VM sandbox.
