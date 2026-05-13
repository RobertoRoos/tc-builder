namespace TcBuilder.Services;

/// <summary>
/// Abstraction over the TwinCAT solution build pipeline.
/// Keeps command handlers free of automation details (DTE, MSBuild, etc.).
/// </summary>
public interface ITwinCatBuildService
{
    Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken);

    Task<BuildResult> CleanAsync(FileInfo solution, CancellationToken cancellationToken);

    Task<SolutionInfo> GetInfoAsync(FileInfo solution, CancellationToken cancellationToken);
}

/// <summary>Inputs for a single build invocation.</summary>
public record BuildRequest(
    FileInfo Solution,
    string Configuration,
    DirectoryInfo? OutputDirectory);

/// <summary>Outcome of a build or clean.</summary>
public record BuildResult(bool Success, string? Message, TimeSpan Elapsed)
{
    public static BuildResult Ok(TimeSpan elapsed, string? message = null) =>
        new(true, message, elapsed);

    public static BuildResult Fail(TimeSpan elapsed, string message) =>
        new(false, message, elapsed);
}

/// <summary>Surface-level metadata about a TwinCAT solution.</summary>
public record SolutionInfo(string Name, string Path, int ProjectCount);
