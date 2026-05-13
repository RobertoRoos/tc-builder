using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace TcBuilder.Services;

/// <summary>
/// Default <see cref="ITwinCatBuildService"/> implementation.
/// This is a scaffold — the real automation (TcXaeShell / Visual Studio DTE)
/// hangs off the TODOs below.
/// </summary>
public class TwinCatBuildService : ITwinCatBuildService
{
    private readonly ILogger<TwinCatBuildService> _logger;

    public TwinCatBuildService(ILogger<TwinCatBuildService> logger)
    {
        _logger = logger;
    }

    public Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken)
    {
        _logger.LogDebug(
            "Building {Solution} ({Configuration}) -> {Output}",
            request.Solution.FullName,
            request.Configuration,
            request.OutputDirectory?.FullName ?? "<default>");

        // TODO: Launch TcXaeShell / VisualStudio.DTE.17.0, open the solution,
        // drive a build, and marshal any errors back into BuildResult.
        Stopwatch sw = Stopwatch.StartNew();
        sw.Stop();

        return Task.FromResult(BuildResult.Fail(
            sw.Elapsed,
            "BuildAsync is not yet implemented. Wire up the TwinCAT automation here."));
    }

    public Task<BuildResult> CleanAsync(FileInfo solution, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Cleaning {Solution}", solution.FullName);

        // TODO: Remove `_Boot`, `_CompileInfo`, `bin/`, `obj/` and any TwinCAT-specific
        // artifacts under the solution directory.
        Stopwatch sw = Stopwatch.StartNew();
        sw.Stop();

        return Task.FromResult(BuildResult.Ok(sw.Elapsed, "Nothing to clean (stub)."));
    }

    public Task<SolutionInfo> GetInfoAsync(FileInfo solution, CancellationToken cancellationToken)
    {
        // TODO: Parse the .sln (or use MSBuild) to enumerate TwinCAT projects
        // and report versions, runtimes, targets, etc.
        SolutionInfo info = new(
            Name: Path.GetFileNameWithoutExtension(solution.Name),
            Path: solution.FullName,
            ProjectCount: 0);

        return Task.FromResult(info);
    }
}
