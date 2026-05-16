# Contributing

## Dependencies

The magical namespace `EnvDTE100` is required.
Such a package was available through NuGet, though its exact purpose and original is unclear.

Another tricky dependency is `TCatSysManagerLib`.
Officially, this is available as a 'COM Reference', which can be set-up through the Visual Studio reference manager.
However, this breaks the .NET Core `dotnet build ...` interface.
So instead the underlying DLL has simply been absorbed into this repository and is linked directly.
