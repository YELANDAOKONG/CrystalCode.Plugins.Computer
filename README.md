# CrystalCode Computer

This is a plugin for [Crystal Code](https://github.com/YELANDAOKONG/CrystalCode). It controls one already-running VirtualBox VM:
status and Guest Additions detection, a display capture, text and key input,
pointer click, drag, and scroll, and one guest process.

Set `CRYSTAL_COMPUTER_VM_UUID` to the UUID of the VM the agent may control.
Optionally set `CRYSTAL_COMPUTER_VBOXMANAGE` to the absolute path of `VBoxManage`.
The VM must already be running. The plugin never accepts a VM identifier from
the model. Tested with VirtualBox 7.2.x.

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
and `plugin.json` together in one directory under `~/.crystal/plugins`. Enable
Plugins in Crystal Code and select a model and provider that support
image input for `computer_observe`. The plugin joins the Work catalog.
`computer_status` and `computer_observe` are reads. The other tools control
the VM and are classified as privileged, so the host approval mode applies.
Before each model call, a hook drops screenshots from this plugin's older
`computer_observe` results. The three newest captures are still sent. The
session keeps every screenshot, and images from any other tool stay on the
request.

`Crystal.Tools` and `CrystalCode.Plugins` are referenced as sibling source
projects. This project does not add NuGet packages. `IComputerAdapter` keeps the tool definitions
independent from VirtualBox. `VBoxManageAdapter` is the first implementation;
it invokes `VBoxManage` without a shell and uses the configured VM UUID for
every call. Pointer input uses the same rule for `python3`. This is in-process
trusted code, not a host-enforced VM sandbox.
