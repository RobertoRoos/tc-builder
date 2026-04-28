using System.CommandLine;
using Microsoft.Extensions.Logging.Abstractions;
using TcBuilder.Commands;
using TcBuilder.Services;
using TcBuilder.Tests.Infrastructure;

namespace TcBuilder.Tests.Commands;

/// <summary>
/// Fast unit tests for the <c>build</c> verb — no TwinCAT, no filesystem
/// beyond an empty placeholder .sln. Verifies that CLI parsing dispatches
/// the right <see cref="BuildRequest"/> to the service.
/// </summary>
public sealed class BuildCommandTests
{
    [Fact]
    public async Task Fails_When_Solution_Does_Not_Exist()
    {
        FakeTwinCatBuildService fake = new();
        RootCommand root = BuildRoot(fake);

        string missing = Path.Combine(Path.GetTempPath(), $"does-not-exist-{Guid.NewGuid():N}.sln");

        int exitCode = await root.Parse(["build", missing]).InvokeAsync();

        exitCode.ShouldBe(1);
        fake.BuildInvocations.ShouldBeEmpty();
    }

    [Fact]
    public async Task Passes_Configuration_Flag_To_Service()
    {
        FakeTwinCatBuildService fake = new();
        RootCommand root = BuildRoot(fake);

        await using TempSolutionFile sln = TempSolutionFile.Create();

        int exitCode = await root.Parse(["build", sln.Path, "-c", "Debug"]).InvokeAsync();

        exitCode.ShouldBe(0);
        fake.BuildInvocations.Count.ShouldBe(1);
        fake.BuildInvocations[0].Configuration.ShouldBe("Debug");
        fake.BuildInvocations[0].Solution.FullName.ShouldBe(sln.Path);
    }

    [Fact]
    public async Task Defaults_Configuration_To_Release()
    {
        FakeTwinCatBuildService fake = new();
        RootCommand root = BuildRoot(fake);

        await using TempSolutionFile sln = TempSolutionFile.Create();

        await root.Parse(["build", sln.Path]).InvokeAsync();

        fake.BuildInvocations[0].Configuration.ShouldBe("Release");
    }

    [Fact]
    public async Task Returns_Non_Zero_When_Build_Fails()
    {
        FakeTwinCatBuildService fake = new()
        {
            NextBuildResult = BuildResult.Fail(TimeSpan.Zero, "boom"),
        };
        RootCommand root = BuildRoot(fake);

        await using TempSolutionFile sln = TempSolutionFile.Create();

        int exitCode = await root.Parse(["build", sln.Path]).InvokeAsync();

        exitCode.ShouldBe(1);
    }

    private static RootCommand BuildRoot(ITwinCatBuildService service)
    {
        BuildCommand command = new(service, NullLogger<BuildCommand>.Instance);
        RootCommand root = new();
        root.Subcommands.Add(command);
        return root;
    }
}
