using System.Runtime.InteropServices;
using EnvDTE;
using EnvDTE100;
using Microsoft.Extensions.Logging;
using TCatSysManagerLib; // This is made available as a "COM Reference" in this VS project
using TcPlcIECProject = TCatSysManagerLib.ITcPlcIECProject7;
using TcSysManager = TCatSysManagerLib.ITcSysManager18;

// For whatever reason these classes have a bunch of numbered sub-classes, we just pick the latest ones

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

    // Privates:
    private EnvDTE.DTE? _dte;
    private EnvDTE.Solution? _solution;
    private EnvDTE.Project? _tcProject;
    private TcPlcIECProject? _plcProject;
    private string _plcProjectName = "<unknown>"; // The name is not accessible from `_plcProject` itself
    private TcSysManager? _sysManager;

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
    protected EnvDTE.DTE DTE
    {
        get
        {
            if (_dte is null)
            {
                _dte = GetOrMakeDTE(IdeVersion);
            }
            return _dte;
        }
    }

    /// <summary>
    /// Property for the DTE.Solution instance
    /// </summary>
    protected EnvDTE.Solution Solution
    {
        get
        {
            if (_solution is null)
            {
                _solution = DTE.Solution;

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
            }
            return _solution;
        }
    }

    /// <summary>
    /// Property for the TwinCAT project (.tsproj)
    /// </summary>
    protected EnvDTE.Project TcProject
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
    protected TcSysManager SysManager
    {
        get
        {
            if (_sysManager is null)
            {
                _sysManager = (TcSysManager)TcProject.Object;
            }
            return _sysManager;
        }
    }

    /// <summary>
    /// Property for the PLC project (.plcproj)
    /// </summary>
    protected TcPlcIECProject PlcProject
    {
        get
        {
            if (_plcProject is null)
            {
                var tipc = SysManager.LookupTreeItem("TIPC");
                var count = tipc.ChildCount;
                if (count != 1)
                {
                    throw new InvalidOperationException(
                        $"Found {count} PLC projects instead of just one, not sure how to continue"
                    );
                }
                // Rather absurd, but we must first determine the exact project name and then retrieve the project again
                // 'tipc.Child[1]` may not be cast to a `TcPlcIECProject` instance
                _plcProjectName = tipc.Child[1].Name;
                var plcItem = SysManager.LookupTreeItem(
                    $"TIPC^{_plcProjectName}^{_plcProjectName} Project"
                );
                _plcProject = (TcPlcIECProject)plcItem;
            }
            return _plcProject;
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
    protected EnvDTE.DTE GetOrMakeDTE(IDEVersion? version)
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
                    return (EnvDTE.DTE)Marshal2.GetActiveObject(progId);
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
                return (EnvDTE.DTE)Activator.CreateInstance(type);
            }
        }

        foreach (IDEVersion ver in Enum.GetValues(typeof(IDEVersion)))
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
    /// 'deconstructor', kind of
    /// </summary>
    public void Dispose()
    {
        if (_dte is not null && !Attach)
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

        _logger.LogInformation($"Building solution ({name})...");

        solutionBuild.Build(true);
    }

    /// <summary>
    /// Perform the 'check all objects' action on the PLC project.
    /// </summary>
    public void CheckObjects()
    {
        var project = PlcProject;
        _logger.LogInformation($"Performing 'check all objects' for '{_plcProjectName}'...");
        bool result = project.CheckAllObjects();
        if (result)
        {
            _logger.LogInformation("No errors found");
        }
        else
        {
            _logger.LogError("Errors found in PLC project!");
        }
    }
}
