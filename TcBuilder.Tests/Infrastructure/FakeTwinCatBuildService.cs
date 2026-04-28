using TcBuilder.Services;

namespace TcBuilder.Tests.Infrastructure;

/// <summary>
/// Hand-rolled test double for <see cref="ITwinCatBuildService"/>.
/// Records the arguments it receives and returns whatever the test configured.
/// </summary>
internal sealed class FakeTwinCatBuildService : ITwinCatBuildService
{
    public List<BuildRequest> BuildInvocations { get; } = new();
    public List<FileInfo> CleanInvocations { get; } = new();
    public List<FileInfo> InfoInvocations { get; } = new();

    public BuildResult NextBuildResult { get; set; } = BuildResult.Ok(TimeSpan.Zero);
    public BuildResult NextCleanResult { get; set; } = BuildResult.Ok(TimeSpan.Zero);
    public SolutionInfo NextInfo { get; set; } = new("stub", "stub", 0);

    public Task<BuildResult> BuildAsync(BuildRequest request, CancellationToken cancellationToken)
    {
        BuildInvocations.Add(request);
        return Task.FromResult(NextBuildResult);
    }

    public Task<BuildResult> CleanAsync(FileInfo solution, CancellationToken cancellationToken)
    {
        CleanInvocations.Add(solution);
        return Task.FromResult(NextCleanResult);
    }

    public Task<SolutionInfo> GetInfoAsync(FileInfo solution, CancellationToken cancellationToken)
    {
        InfoInvocations.Add(solution);
        return Task.FromResult(NextInfo);
    }
}
