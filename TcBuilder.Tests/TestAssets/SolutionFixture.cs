using System.Linq.Expressions;
using System.Reflection;

namespace TcBuilder.Tests.TestAssets;

/// <summary>
/// Tests fixture to assemble a TwinCAT solution for test.
///
/// For each usage it does the following:
/// - Copy an entire solution folder to some writable temp-directory
/// - Remove that folder when finished
///
/// This allows us to build, clean, rebuild, etc. in tests without affecting other tests.
/// 
/// Note that the sources are _not_ copied from the repository root directly. Instead,
/// the test assets are copied into the build directory during the compilation, through
/// the `CopyToOutputDirectory` file property. This guarentees the location of the assets
/// w.r.t. the active tests binary.
/// </summary>
public class SolutionFixture : IDisposable
{
    static readonly string tempTestsRootName = "tc_builder_tests";
    static DirectoryInfo? tempTestsRoot = null;

    public DirectoryInfo TestsDirectory { get; private set; }
    public string Name { get; private set; }

    public FileInfo SolutionFile
    {
        get { return new(GetPath($"{Name}.sln")); }
    }

    /// <summary>
    /// Constructor - does the necessary copy-actions.
    /// </summary>
    protected SolutionFixture(string folderName)
    {
        if (tempTestsRoot is null) // Static init
        {
            string dateStr = DateTime.Now.ToString("yyyy_MM_dd-hhmmssfff");
            tempTestsRoot = new(Path.Join(Path.GetTempPath(), tempTestsRootName, dateStr));
            tempTestsRoot.Create();
        }

        DirectoryInfo sourceDir = new(
            Path.Join(AppContext.BaseDirectory, "TestAssets", folderName)
        );
        TestsDirectory = new(Path.Join(tempTestsRoot!.FullName, folderName));
        Name = folderName;

        CopyFilesRecursively(sourceDir.FullName, TestsDirectory.FullName);
    }

    /// <summary>
    /// Deconstructor
    /// </summary>
    public void Dispose()
    {
        if (TestsDirectory is not null && TestsDirectory.Exists)
        {
            TestsDirectory.Delete(true);
        }
    }

    /// <summary>
    /// Class-level teardown.
    /// </summary>
    public static void CleanUp()
    {
        if (tempTestsRoot is not null && tempTestsRoot.Exists)
        {
            tempTestsRoot.Delete();
        }
    }

    /// <summary>
    /// Resolve path under this fixture.
    /// </summary>
    public string GetPath(params string[] elements)
    {
        var path = TestsDirectory.FullName;
        foreach (var element in elements)
        {
            path = Path.Join(path, element);
        }
        return path;
    }

    /// <summary>
    /// Start a new fixture.
    /// </summary>
    public static SolutionFixture Load(string folderName)
    {
        return new(folderName);
    }

    /// <summary>
    /// C# has no neat built-in function to copy entire folders.
    /// </summary>
    private static void CopyFilesRecursively(string sourcePath, string targetPath)
    {
        //Now Create all of the directories
        foreach (
            string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories)
        )
        {
            Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
        }

        //Copy all the files & Replaces any files with the same name
        foreach (
            string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories)
        )
        {
            File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
        }
    }
}
