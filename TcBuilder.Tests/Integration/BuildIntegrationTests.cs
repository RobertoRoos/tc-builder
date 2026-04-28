using Microsoft.Extensions.Logging.Abstractions;
using TcBuilder.Services;
using TcBuilder.Tests.Infrastructure;

namespace TcBuilder.Tests.Integration;

/// <summary>
/// End-to-end tests that exercise the real <see cref="TwinCatBuildService"/>
/// against a fixture TwinCAT solution copied to a fresh temp directory.
/// These require TwinCAT / TcXaeShell on the machine running the tests and
/// stay skipped until <c>TestAssets/MinimalTwinCatSolution/</c> is populated.
/// </summary>
[Trait("Category", "Integration")]
public sealed class BuildIntegrationTests
{
    private const string MinimalAsset = "MinimalTwinCatSolution";

    [Fact(Skip = "Enable once TcBuilder.Tests/TestAssets/MinimalTwinCatSolution is populated.")]
    public async Task Builds_The_Minimal_Fixture_Solution()
    {
        await using TwinCatProjectFixture fixture = TwinCatProjectFixture.Load(MinimalAsset);

        TwinCatBuildService service = new(NullLogger<TwinCatBuildService>.Instance);

        BuildRequest request = new(
            Solution: fixture.SolutionFile,
            Configuration: "Release",
            OutputDirectory: null);

        BuildResult result = await service.BuildAsync(request, TestContext.Current.CancellationToken);

        result.Success.ShouldBeTrue(result.Message);
    }

    [Fact(Skip = "Enable once TcBuilder.Tests/TestAssets/MinimalTwinCatSolution is populated.")]
    public async Task Clean_Then_Build_Yields_A_Fresh_Artifact()
    {
        await using TwinCatProjectFixture fixture = TwinCatProjectFixture.Load(MinimalAsset);

        TwinCatBuildService service = new(NullLogger<TwinCatBuildService>.Instance);
        CancellationToken ct = TestContext.Current.CancellationToken;

        BuildResult clean = await service.CleanAsync(fixture.SolutionFile, ct);
        clean.Success.ShouldBeTrue(clean.Message);

        BuildResult build = await service.BuildAsync(
            new BuildRequest(fixture.SolutionFile, "Release", null),
            ct);

        build.Success.ShouldBeTrue(build.Message);
    }
}
