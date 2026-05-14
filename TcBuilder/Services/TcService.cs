using EnvDTE100;
using Microsoft.Extensions.Logging;

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
    TcXAEx64, // 64-bit
    TcXAE, // 32-bit
}

/// <summary>
/// Helper-object to interface with Visual Studio / TwinCAT.
///
/// Each instance of the helper will store DTE instances, solutions, projects,
/// etc.
///
/// It is used as a singleton in the application framework.
/// </summary>
public class TcService
{
    private EnvDTE.DTE? _dte;
    private readonly ILogger<TcService> _logger;

    public TcService(ILogger<TcService> logger)
    {
        _logger = logger;
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
            case IDEVersion.TcXAEx64:
                return "TcXaeShell.DTE.17.0";
            case IDEVersion.TcXAE:
                return "TcXaeShell.DTE.15.0";
            default:
                throw new ArgumentException("Unknown IDE verson", nameof(version));
        }
    }

    /// <summary>
    /// Return a new DTE instance.
    /// </summary>
    protected EnvDTE.DTE MakeDte(IDEVersion? version)
    {
        if (version is IDEVersion v)
        {
            var progId = GetProgId(v);
            _logger.LogDebug($"Trying to make DTE for {v} ({progId})...");
            Type? type = Type.GetTypeFromProgID(progId);
            if (type is null)
            {
                throw new InvalidOperationException(
                    $"Could not make DTE instance of type '{progId}' ({v})"
                );
            }
            return (EnvDTE.DTE)Activator.CreateInstance(type);
        }

        foreach (IDEVersion ver in Enum.GetValues(typeof(IDEVersion)))
        {
            try
            {
                return MakeDte(ver);
            }
            catch (InvalidOperationException) { }
        }
        throw new InvalidOperationException("Failed to initialize any DTE");
    }

    /// <summary>
    /// Prepare a DTE instance.
    /// </summary>
    public void InitDte(IDEVersion? version)
    {
        _dte = MakeDte(version);
        _logger.LogDebug("Created DTE instance");
    }

    ~TcService()
    {
        if (_dte is not null)
        {
            _dte.Quit();
            _dte = null;
        }
    }
}
