using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder run ...</c>
/// </summary>
public class RunCommand : Command
{
    public RunCommand(ITwinCatBuildService buildService, ILogger<RunCommand> logger)
        : base("run", "Run a sequence on a TwinCAT solution")
    {
        Option<string[]> stepsOption = new("--steps", "-s")
        {
            Description = "Actions to do for this target, in order.",
            AllowMultipleArgumentsPerToken = true,
            Required = true,
        };

        Option<FileInfo?> solutionOption = new("--solution", "-i")
        {
            Description = "Path to a solution containing a TwinCAT project.",
        };

        Options.Add(stepsOption);
        Options.Add(solutionOption);

        SetAction(async (parseResult, cancellationToken) =>
        {
            string[] steps = parseResult.GetValue(stepsOption)!;
            var solution = parseResult.GetValue(solutionOption);

            logger.LogInformation(solution.ToString());

            foreach (string step in steps)
            {
                logger.LogInformation(step);
            }

            logger.LogInformation("Hi!");
            return 0;
        });
    }
}
