using System.CommandLine;
using Microsoft.Extensions.Logging;
using TcBuilder.Services;

namespace TcBuilder.Commands;

/// <summary>
/// Possible 'step' values
/// </summary>
public enum Step
{
    build,
    check,
    activate,
}

/// <summary>
/// <c>tcbuilder run ...</c>
/// </summary>
public class RunCommand : Command
{
    public RunCommand(TcService tc, ILogger<RunCommand> logger)
        : base("run", "Run a sequence on a TwinCAT solution")
    {
        Option<Step[]> stepsOption = new("--steps", "-s")
        {
            Description = "Actions to do for this target, in order.\n" + "Valid options are: ",
            AllowMultipleArgumentsPerToken = true,
        };
        stepsOption.Description += string.Join(
            ", ",
            Enum.GetValues<Step>().Select(step => $"'{step}'")
        );
        Options.Add(stepsOption);

        Option<FileInfo?> solutionOption = new("--solution", "-i")
        {
            Description = "Path to a solution containing a TwinCAT project.",
        };
        Options.Add(solutionOption);

        Option<string?> plcNameOption = new("--plc", "-p")
        {
            Description = "Exact name of the PLC project (default: detected automatically)",
        };
        Options.Add(plcNameOption);

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

        Option<bool> showUIOption = new("--show-ui", "-u")
        {
            Description = "Do not hide / surpress the IDE instance (which is the default)",
        };
        Options.Add(showUIOption);

        Option<bool> keepOpenOption = new("--keep-open", "-k")
        {
            Description =
                "Do not close the instance when done (only meaningful in combination with '--keep-open')\n"
                + "(this is already implied when using '--attach')",
        };
        Options.Add(keepOpenOption);

        Option<bool> warningsOption = new("--warnings-as-errors", "-W")
        {
            Description =
                "Treat warnings as errors, potentially failing otherwise succeeding builds",
        };
        Options.Add(warningsOption);

        SetAction(
            async (parseResult, cancellationToken) =>
            {
                // CLI arguments:

                var solution = parseResult.GetValue(solutionOption);

                // Set info:
                tc.IdeVersion = parseResult.GetValue(ideOption);
                tc.Attach = parseResult.GetValue(attachOption);
                tc.SolutionFile = parseResult.GetValue(solutionOption);
                if (parseResult.GetValue(plcNameOption) is string name)
                {
                    tc.PlcProjectName = name;
                }
                tc.KeepOpen = parseResult.GetValue(keepOpenOption);
                tc.ShowUI = parseResult.GetValue(showUIOption);
                tc.WarningsAsError = parseResult.GetValue(warningsOption);

                await Task.Run(() =>
                {
                    foreach (var step in parseResult.GetValue(stepsOption)!)
                    {
                        switch (step)
                        {
                            case Step.build:
                                tc.Build();
                                break;
                            case Step.check:
                                tc.CheckObjects();
                                break;
                            case Step.activate:
                                tc.Activate();
                                break;
                            default:
                                throw new InvalidOperationException($"Unknown step '{step}'");
                        }
                    }
                });

                return 0;
            }
        );
    }
}
