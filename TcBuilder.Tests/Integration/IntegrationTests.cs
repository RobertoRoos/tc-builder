using Microsoft.Extensions.Logging.Abstractions;
using TcBuilder.Services;
using TcBuilder.Tests.TestAssets;

namespace TcBuilder.Tests.Integration;

/// <summary>
/// End-to-end tests that exercise the real <see cref="TcService"/>.
///
/// This is not a true unit-test testcase, it is full integration test. It
/// depends on a real TwinCAT IDE being present and it will perform real TwinCAT builds.
/// </summary>
[Trait("Category", "Integration")]
public sealed class IntegrationTests : IDisposable
{
    SolutionFixture? solution = null;
    TcService? tc = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public IntegrationTests()
    {
        // empty
    }

    /// <summary>
    /// Finalizer (deconstructor)
    /// </summary>
    public void Dispose()
    {
        if (tc is not null)
        {
            tc.Dispose(); // Close the DTE before trying to delete the folder
            tc = null;
        }

        if (solution is not null)
        {
            solution.Dispose();
            solution = null;
        }
    }

    [Theory]
    [InlineData(IDEVersion.TcXAE)]
    [InlineData(IDEVersion.VS2022)]
    public void Happy_Flow_Build(IDEVersion ide)
    {
        solution = SolutionFixture.Load("minimal");
        tc = MakeService();

        tc.IdeVersion = ide;
        tc.SolutionFile = solution.SolutionFile;
        tc.ShowUI = true;
        tc.Build();
    }

    [Fact]
    public void Dummy()
    {
        solution = SolutionFixture.Load("minimal");
    }

    private TcService MakeService()
    {
        NullLogger<TcService> logger = new();
        return new(logger);
    }
}
