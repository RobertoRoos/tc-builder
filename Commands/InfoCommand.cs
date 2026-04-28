using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder info &lt;solution&gt;</c>
/// </summary>
internal sealed class InfoCommand : Command
{
    public InfoCommand(ITwinCatBuildService buildService, ILogger<InfoCommand> logger)
        : base("info", "Print information about a TwinCAT solution.")
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

            SolutionInfo info = await buildService.GetInfoAsync(solution, cancellationToken);

            Console.WriteLine($"Solution : {info.Name}");
            Console.WriteLine($"Path     : {info.Path}");
            Console.WriteLine($"Projects : {info.ProjectCount}");
            return 0;
        });
    }
}
