# Test assets

Drop fixture TwinCAT solutions here.
Each subfolder is treated as a named asset that tests load by name:

```csharp
var fixture = SolutionFixture.Load("<name>");
```

## Available Fixtures

- `minimal`
  -	Very base TwinCAT + PLC project.
	Should compile without errors or warnings. 
