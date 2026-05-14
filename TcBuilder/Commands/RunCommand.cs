using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder run ...</c>
/// </summary>
public class RunCommand : Command
{
    public RunCommand(TwinCatHelper tc, ILogger<RunCommand> logger)
        : base("run", "Run a sequence on a TwinCAT solution")
    {
        Option<string[]> stepsOption = new("--steps", "-s")
        {
            Description = "Actions to do for this target, in order.",
            AllowMultipleArgumentsPerToken = true,
        };
        Options.Add(stepsOption);

        Option<FileInfo?> solutionOption = new("--solution", "-i")
        {
            Description = "Path to a solution containing a TwinCAT project.",
        };
        Options.Add(solutionOption);

        Option<IDEVersion?> ideOption = new("--ide")
        {
            Description = "IDE version to use (default: use the first that can be located)",
        };
        Options.Add(ideOption);

        SetAction(
            async (parseResult, cancellationToken) =>
            {
                // CLI arguments:
                string[] steps = parseResult.GetValue(stepsOption)!;
                var solution = parseResult.GetValue(solutionOption);

                // Prepare environment:
                tc.InitDte(parseResult.GetValue(ideOption));

                return 0;
            }
        );
    }
}
