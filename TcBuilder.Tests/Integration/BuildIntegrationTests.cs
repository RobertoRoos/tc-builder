using Microsoft.Extensions.Logging.Abstractions;
using TcBuilder.Services;

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
        ;
    }

    [Fact(Skip = "Enable once TcBuilder.Tests/TestAssets/MinimalTwinCatSolution is populated.")]
    public async Task Clean_Then_Build_Yields_A_Fresh_Artifact()
    {
        ;
    }
}
