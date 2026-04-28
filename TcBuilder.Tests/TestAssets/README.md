# Test assets

Drop fixture TwinCAT solutions here. Each subfolder is treated as a named
asset that tests load by name:

```csharp
await using var fixture = TwinCatProjectFixture.Load("MinimalTwinCatSolution");
// fixture.SolutionFile is the copied .sln in a fresh temp directory
// the directory is deleted on DisposeAsync
```

Everything under `TestAssets/` is copied next to the test assembly at build
time (see the `<None Include="TestAssets\**\*" />` item in
`TcBuilder.Tests.csproj`). At runtime the fixture resolves assets relative to
`AppContext.BaseDirectory`, so the copy-to-output mechanism is what makes the
layout below discoverable.

## Expected contents

### `MinimalTwinCatSolution/` (TODO)

A stripped-down TwinCAT solution with a single PLC project — enough for
`TwinCatBuildService.BuildAsync` to exercise the full open-solution /
build-solution automation path.

Once populated, remove the `Skip = "..."` from the tests in
`Integration/BuildIntegrationTests.cs`.

## Adding more fixtures

Any additional folder under `TestAssets/` becomes a new asset name. For
example, a `LargeMotionSolution/` folder would be loaded with
`TwinCatProjectFixture.Load("LargeMotionSolution")`.
