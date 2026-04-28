using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder build &lt;solution&gt; [--configuration] [--output]</c>
/// </summary>
internal sealed class BuildCommand : Command
{
    public BuildCommand(ITwinCatBuildService buildService, ILogger<BuildCommand> logger)
        : base("build", "Build a TwinCAT solution.")
    {
        Argument<FileInfo> solutionArg = new("solution")
        {
            Description = "Path to the TwinCAT .sln file.",
        };

        Option<string> configurationOption = new("--configuration", "-c")
        {
            Description = "Build configuration (e.g. Debug or Release).",
            DefaultValueFactory = _ => "Release",
        };

        Option<DirectoryInfo?> outputOption = new("--output", "-o")
        {
            Description = "Directory to place build output in.",
        };

        Arguments.Add(solutionArg);
        Options.Add(configurationOption);
        Options.Add(outputOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            FileInfo solution = parseResult.GetValue(solutionArg)!;
            string configuration = parseResult.GetValue(configurationOption)!;
            DirectoryInfo? output = parseResult.GetValue(outputOption);

            if (!solution.Exists)
            {
                logger.LogError("Solution file not found: {Path}", solution.FullName);
                return 1;
            }

            logger.LogInformation(
                "Building {Solution} ({Configuration})",
                solution.Name,
                configuration);

            BuildRequest request = new(solution, configuration, output);
            BuildResult result = await buildService.BuildAsync(request, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation(
                    "Build succeeded in {Elapsed:mm\\:ss\\.fff}.",
                    result.Elapsed);
                return 0;
            }

            logger.LogError("Build failed: {Message}", result.Message);
            return 1;
        });
    }
}
