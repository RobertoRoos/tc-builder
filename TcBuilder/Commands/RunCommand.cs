using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// <c>tcbuilder run ...</c>
/// </summary>
public class RunCommand : Command
{
    public RunCommand(TcService tc, ILogger<RunCommand> logger)
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

        Option<bool> attachOption = new("--attach")
        {
            Description = "Attach to a running IDE instance instead of opening a new one",
        };
        Options.Add(attachOption);

        SetAction(
            async (parseResult, cancellationToken) =>
            {
                // CLI arguments:

                var solution = parseResult.GetValue(solutionOption);

                // Set info:
                tc.IdeVersion = parseResult.GetValue(ideOption);
                tc.Attach = parseResult.GetValue(attachOption);
                tc.SolutionFile = parseResult.GetValue(solutionOption);

                foreach (string step in parseResult.GetValue(stepsOption)!)
                {
                    switch (step)
                    {
                        case "build":
                            tc.Build();
                            break;
                        case "check-objects":
                            tc.CheckObjects();
                            break;
                        default:
                            throw new InvalidOperationException($"Unknown step '{step}'");
                    }
                }

                return 0;
            }
        );
    }
}
