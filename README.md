# TcBuilder

**Status:**  
This project under active development and not finished.  
However, it should already be functional for basic usage.

---

This repository contains a program to interface with TwinCAT over the commandline.
TwinCAT is notoriously difficult to automate and this tool should be the final solution for it.

It is written in C#.
Automation is also possible through PowerShell.
Using scripts seems preferable, but the PowerShell interface is rather buggy and unstable, the C# version is much more robust.

It works through the DTE COM-object interface.
Effectively an instance of Visual Studio or the TwinCAT XAE Shell is created (or an existing instance is taken over) and used to build, activate, etc.

See [Contributing](CONTRIBUTING.md) for implementation details.
