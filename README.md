# Crystal Computer

This is an independent native dotnet tool set for Crystal Code. The current
VirtualBox adapter can report VM state, capture a display, type text, and press
keys or shortcuts. Mouse
input and guest process execution are not implemented yet.

Set `CRYSTAL_COMPUTER_VM_UUID` to the UUID of the VM the agent may control.
Optionally set `CRYSTAL_COMPUTER_VBOXMANAGE` to the absolute path of `VBoxManage`.
The VM must already be running. The tool set never accepts a VM identifier from
the model.

Publish `Crystal.Computer/Crystal.Computer.csproj` and place the published files
and `tools.json` together in one directory under `~/.crystal/tools`. Enable
External Tools in Crystal Code and select a model and provider that support
image input for `computer_observe`. The manifest enables the Work catalog and
declares `approval: always`. That declaration takes effect when Crystal Code's
Home external tool approval source is `author`.

The .NET tool contracts are referenced as sibling source projects. This project
does not add NuGet packages. `IComputerAdapter` keeps the tool definitions
independent from VirtualBox. `VBoxManageAdapter` is the first implementation;
it invokes `VBoxManage` without a shell and uses the configured VM UUID for
every call. This is in-process trusted code, not a host-enforced VM sandbox.
