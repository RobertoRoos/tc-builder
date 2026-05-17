using System.ComponentModel;
using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE100;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using TCatSysManagerLib; // This is made available as a "COM Reference" in this VS project
//
// For whatever reason these classes have a bunch of numbered sub-classes, we just pick the latest ones:
using TypeDTE = EnvDTE80.DTE2;
using TypePlcIECProject = TCatSysManagerLib.ITcPlcIECProject7;
using TypePlcProject = TCatSysManagerLib.ITcPlcProject; // There is IECProject and just Project and they are different...
using TypeProject = EnvDTE.Project;
using TypeSolution = EnvDTE100.Solution4;
using TypeSysManager = TCatSysManagerLib.ITcSysManager18;

namespace TcBuilder.Services;

/// <summary>
/// Possible IDE versions.
///
/// Used to resolve DTE strings. The order here is also the order of instanstaion
/// preference.
/// </summary>
public enum IDEVersion
{
    // Visual Studio versions:
    VS2022,
    VS2019,
    VS2017,

    // TwinCAT XAE shell:
    TcXAE, // 64-bit (very like prefered over the 32 bit, so use the short name)
    TcXAE32, // 32-bit
}

/// <summary>
/// Helper-object to interface with Visual Studio / TwinCAT.
///
/// Each instance of the helper will store DTE instances, solutions, projects,
/// etc.
/// These are all loaded lazy through properties.
///
/// It is used as a singleton in the application framework.
/// </summary>
public class TcService : IDisposable
{
    // Inputs:
    public IDEVersion? IdeVersion { get; set; }
    public bool Attach { get; set; } = false;
    public FileInfo? SolutionFile { get; set; }
    public bool KeepOpen { get; set; } = false;
    public bool ShowUI { get; set; } = false;
    public bool WarningsAsError { get; set; } = false;

    // Privates:
    private TypeDTE? _dte;
    private TypeSolution? _solution;
    private TypeProject? _tcProject;
    private TypePlcIECProject? _plcProject;
    private string? _plcProjectName; // The name is not accessible from `_plcProject` itself
    private TypePlcProject? _plcRootProject;
    private TypeSysManager? _sysManager;
    private OutputWindowPane? _buildOutput;
    private EditPoint? _buildOutputStartPoint; // Head-end of new build output

    private readonly ILogger<TcService> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public TcService(ILogger<TcService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Property for the DTE instance
    /// </summary>
    protected TypeDTE DTE
    {
        get
        {
            if (_dte is null)
            {
                _dte = GetOrMakeDTE(IdeVersion);

                ConsumeBuildOutput(); // As the DTE is new, reset the build output head
            }
            return _dte;
        }
    }

    /// <summary>
    /// Property for the DTE.Solution instance
    /// </summary>
    protected TypeSolution Solution
    {
        get
        {
            if (_solution is null)
            {
                _solution = (TypeSolution)DTE.Solution;

                if (SolutionFile is FileInfo file)
                {
                    var path = file.FullName;
                    if (_solution.IsOpen)
                    {
                        var currentPath = _solution.FullName;
                        _logger.LogDebug($"Currently opened solution: '{currentPath}'");
                        if (currentPath != path)
                        {
                            throw new InvalidOperationException(
                                $"Not opening solution '{path}' because this instance already has a different solution opened: '{currentPath}'"
                            );
                        }
                    }
                    else
                    {
                        _logger.LogInformation($"Opening solution file '{path}'...");
                        _solution.Open(path);
                    }
                }

                if (!_solution.IsOpen)
                {
                    throw new InvalidOperationException(
                        "No solution opened or selected, cannot continue"
                    );
                }
            }
            return _solution;
        }
    }

    /// <summary>
    /// Property for the TwinCAT project (.tsproj)
    /// </summary>
    protected TypeProject TcProject
    {
        get
        {
            if (_tcProject is null)
            {
                var count = Solution.Projects.Count;
                if (count != 1)
                {
                    throw new InvalidOperationException(
                        $"Solution has {count} projects instead of just one, unsure how to continue"
                    );
                }
                _tcProject = Solution.Projects.Item(1);
            }
            return _tcProject;
        }
    }

    /// <summary>
    /// Property for the internal system manager object.
    /// </summary>
    protected TypeSysManager SysManager
    {
        get
        {
            if (_sysManager is null)
            {
                _sysManager = (TypeSysManager)TcProject.Object;
            }
            return _sysManager;
        }
    }

    /// <summary>
    /// Property for the PLC project (.plcproj)
    /// </summary>
    protected TypePlcIECProject PlcProject
    {
        get
        {
            if (_plcProject is null)
            {
                var plcItem = SysManager.LookupTreeItem(
                    $"TIPC^{PlcProjectName}^{PlcProjectName} Project"
                );
                _plcProject = (TypePlcIECProject)plcItem;
            }
            return _plcProject;
        }
    }

    /// <summary>
    /// Property for the alternative type of a PLC project
    /// </summary>
    protected TypePlcProject PlcRootProject
    {
        get
        {
            if (_plcRootProject is null)
            {
                // Without the second "^<name> Project":
                var plcItem = SysManager.LookupTreeItem($"TIPC^{PlcProjectName}");
                _plcRootProject = (TypePlcProject)plcItem;
            }
            return _plcRootProject;
        }
    }

    /// <summary>
    /// Public property for PLC project name.
    ///
    /// If never specified, look for it automatically.
    /// </summary>
    public string PlcProjectName
    {
        get
        {
            if (_plcProjectName is null)
            {
                var tipc = SysManager.LookupTreeItem("TIPC");
                var count = tipc.ChildCount;
                if (count != 1)
                {
                    throw new InvalidOperationException(
                        $"Found {count} PLC projects instead of just one, not sure how to continue"
                    );
                }
                // Rather absurd, but we must first determine the exact project name and then retrieve the project again.
                // 'tipc.Child[1]` may _not_ be cast to a `TcPlcIECProject` instance
                _plcProjectName = tipc.Child[1].Name;
            }
            return _plcProjectName;
        }
        set { _plcProjectName = value; }
    }

    /// <summary>
    /// Property for the build output window pane.
    /// </summary>
    protected OutputWindowPane BuildOutput
    {
        get
        {
            if (_buildOutput is null)
            {
                var outputPanes = DTE.ToolWindows.OutputWindow.OutputWindowPanes;
                _buildOutput = outputPanes.Item("build");
            }
            return _buildOutput;
        }
    }

    /// <summary>
    /// Get ProgId string from an IDE version enum.
    /// </summary>
    public static string GetProgId(IDEVersion version)
    {
        switch (version)
        {
            case IDEVersion.VS2022:
                return "VisualStudio.DTE.17.0";
            case IDEVersion.VS2019:
                return "VisualStudio.DTE.16.0";
            case IDEVersion.VS2017:
                return "VisualStudio.DTE.15.0";
            case IDEVersion.TcXAE:
                return "TcXaeShell.DTE.17.0";
            case IDEVersion.TcXAE32:
                return "TcXaeShell.DTE.15.0";
            default:
                throw new ArgumentException("Unknown IDE verson", nameof(version));
        }
    }

    /// <summary>
    /// Return a new DTE instance.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Usage",
        "CA1416:Not reachable on other targets than Windows",
        Justification = "Windows-only app"
    )]
    protected TypeDTE GetOrMakeDTE(IDEVersion? version)
    {
        if (version is IDEVersion v)
        {
            var progId = GetProgId(v);
            string infoStr = $"'{v}' ({progId})";
            if (Attach)
            {
                _logger.LogDebug($"Trying to attach to DTE for {infoStr}...");
                try
                {
                    return (TypeDTE)Marshal2.GetActiveObject(progId);
                }
                catch (COMException)
                {
                    throw new InvalidOperationException(
                        $"Could not attach to DTE instance of type {infoStr}"
                    );
                }
            }
            else
            {
                _logger.LogDebug($"Trying to make DTE for {infoStr}...");
                Type? type = Type.GetTypeFromProgID(progId);
                if (type is null)
                {
                    throw new InvalidOperationException(
                        $"Could not make DTE instance of type {infoStr}"
                    );
                }
                var dte = (TypeDTE)Activator.CreateInstance(type)!;
                dte.SuppressUI = !ShowUI;
                dte.MainWindow.Visible = ShowUI;
                return dte;
            }
        }

        foreach (IDEVersion ver in Enum.GetValues<IDEVersion>())
        {
            try
            {
                return GetOrMakeDTE(ver);
            }
            catch (InvalidOperationException) { } // Let slide and try the next option
        }
        throw new InvalidOperationException("Failed to initialize any DTE");
    }

    /// <summary>
    /// Get items from the error-list window and output it.
    ///
    /// This output is always up-to-date with e.g. the last build, no need to wait.
    ///
    /// Returns `true` if there were any errors (respecting `WarningsAsErrors`.
    /// </summary>
    protected bool LogErrorsList()
    {
        DTE.ToolWindows.ErrorList.ShowMessages = false;
        DTE.ToolWindows.ErrorList.ShowWarnings = true;
        DTE.ToolWindows.ErrorList.ShowErrors = true;
        var errorItems = DTE.ToolWindows.ErrorList.ErrorItems;

        bool hasErrors = false;

        for (int i = 1; i <= errorItems.Count; i++)
        {
            var item = errorItems.Item(i);
            bool isError = item.ErrorLevel >= vsBuildErrorLevel.vsBuildErrorLevelHigh;
            string label = isError ? "ERROR" : "WARNING";
            FileInfo file = new(item.FileName);

            _logger.Log(
                isError ? LogLevel.Error : LogLevel.Warning,
                $"{label}:\t{item.Description}\t{file.Name}:{item.Line}"
            );

            if (isError || WarningsAsError)
            {
                hasErrors = true;
            }
        }

        return hasErrors;
    }

    /// <summary>
    /// Get any new build output (only new text since the last call).
    ///
    /// The build output is _not_ instantaneous: after a build the output is not guaranteed
    /// to be aleady up-to-date.
    /// </summary>
    protected string ConsumeBuildOutput()
    {
        if (_buildOutputStartPoint is null)
        {
            _buildOutputStartPoint = BuildOutput.TextDocument.StartPoint.CreateEditPoint();
        }
        var outputEnd = BuildOutput.TextDocument.EndPoint.CreateEditPoint();
        var result = _buildOutputStartPoint.GetText(outputEnd);
        _buildOutputStartPoint = outputEnd;
        return result;
    }

    /// <summary>
    /// 'deconstructor', kind of
    /// </summary>
    public void Dispose()
    {
        if (_dte is not null && !Attach && !KeepOpen)
        {
            _logger.LogDebug("Closing IDE...");
            _dte.Quit();
            _dte = null;
        }
    }

    /// <summary>
    /// Compile the solution.
    /// </summary>
    public void Build()
    {
        var solutionBuild = Solution.SolutionBuild; // Do first so the logging order is correct

        var conf = solutionBuild.ActiveConfiguration;
        var context = conf.SolutionContexts.Item(1);
        string name = conf.Name + "|" + context.PlatformName;

        _logger.LogInformation($"Building solution [{name}]...");

        solutionBuild.Build(true);

        var hasErrors = LogErrorsList();

        if (solutionBuild.LastBuildInfo == 0 && !hasErrors)
        {
            _logger.LogInformation("Succesfully built solution without errors");
        }
        else
        {
            _logger.LogError("Failed to build enitre solution");
        }
    }

    /// <summary>
    /// Perform the 'check all objects' action on the PLC project.
    /// </summary>
    public void CheckObjects()
    {
        var project = PlcProject;

        _logger.LogInformation($"Performing \"check all objects\" for '{_plcProjectName}'...");
        bool result = project.CheckAllObjects();

        var hasErrors = LogErrorsList();

        if (result || hasErrors)
        {
            _logger.LogInformation("No errors found");
        }
        else
        {
            _logger.LogError("Errors found in PLC project!");
        }
    }

    /// <summary>
    /// Same as clicking 'Activate Configuration' in TwinCAT, also downloads PLC project.
    /// </summary>
    public void Activate()
    {
        var netId = SysManager.GetTargetNetId();

        _logger.LogInformation($"Activating configuration on '{netId}'...");

        // This is the same as activating with the 'autostart PLC boot project(s)' option unchecked:
        SysManager.ActivateConfiguration();

        _logger.LogInformation("Making PLC boot project...");

        PlcRootProject.BootProjectAutostart = true;
        PlcRootProject.GenerateBootProject(true);

        if (LogErrorsList())
        {
            _logger.LogError("Encountered errors during activation");
            return;
        }

        SysManager.StartRestartTwinCAT();
        // Unfortunately there is no feedback on the activate, boot project generation or run mode toggle

        if (!SysManager.IsTwinCATStarted())
        {
            _logger.LogError("TwinCAT is not actually in run mode");
        }

        if (LogErrorsList())
        {
            _logger.LogError("Encountered errors during TwinCAT restart");
        }
    }
}
