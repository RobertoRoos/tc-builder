using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder clean &lt;solution&gt;</c>
/// </summary>
internal sealed class CleanCommand : Command
{
    public CleanCommand(ITwinCatBuildService buildService, ILogger<CleanCommand> logger)
        : base("clean", "Remove build artifacts from a TwinCAT solution.")
    {
        Argument<FileInfo> solutionArg = new("solution")
        {
            Description = "Path to the TwinCAT .sln file.",
        };

        Arguments.Add(solutionArg);

        SetAction(async (parseResult, cancellationToken) =>
        {
            FileInfo solution = parseResult.GetValue(solutionArg)!;

            if (!solution.Exists)
            {
                logger.LogError("Solution file not found: {Path}", solution.FullName);
                return 1;
            }

            logger.LogInformation("Cleaning {Solution}", solution.Name);

            BuildResult result = await buildService.CleanAsync(solution, cancellationToken);

            if (result.Success)
            {
                logger.LogInformation(
                    "Clean complete in {Elapsed:mm\\:ss\\.fff}.",
                    result.Elapsed);
                return 0;
            }

            logger.LogError("Clean failed: {Message}", result.Message);
            return 1;
        });
    }
}
