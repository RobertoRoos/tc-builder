namespace TcBuilder.Tests.Infrastructure;

/// <summary>
/// Copies a named TwinCAT test asset into a fresh temp directory and cleans
/// it up on disposal. Assets live under <c>TestAssets/</c> in the test
/// project and are copied next to the test assembly at build time.
/// </summary>
/// <example>
/// <code>
/// await using var fixture = TwinCatProjectFixture.Load("MinimalTwinCatSolution");
/// var result = await service.BuildAsync(
///     new BuildRequest(fixture.SolutionFile, "Release", null),
///     CancellationToken.None);
/// </code>
/// </example>
internal sealed class TwinCatProjectFixture : IAsyncDisposable
{
    /// <summary>The fresh temp directory containing the copied asset.</summary>
    public DirectoryInfo WorkingDirectory { get; }

    /// <summary>The first <c>.sln</c> discovered under <see cref="WorkingDirectory"/>.</summary>
    public FileInfo SolutionFile { get; }

    private TwinCatProjectFixture(DirectoryInfo workingDirectory, FileInfo solutionFile)
    {
        WorkingDirectory = workingDirectory;
        SolutionFile = solutionFile;
    }

    /// <summary>
    /// Load the asset at <c>TestAssets/{assetName}/</c> into a new temp dir.
    /// </summary>
    public static TwinCatProjectFixture Load(string assetName)
    {
        string source = Path.Combine(AppContext.BaseDirectory, "TestAssets", assetName);
        if (!Directory.Exists(source))
        {
            throw new DirectoryNotFoundException(
                $"Test asset '{assetName}' not found at '{source}'. " +
                $"Drop a TwinCAT solution into TcBuilder.Tests/TestAssets/{assetName}/ and rebuild.");
        }

        string destination = Path.Combine(
            Path.GetTempPath(),
            "tcbuilder-tests",
            Guid.NewGuid().ToString("N"));

        CopyDirectory(source, destination);

        string[] solutions = Directory.GetFiles(destination, "*.sln", SearchOption.AllDirectories);
        if (solutions.Length == 0)
        {
            throw new FileNotFoundException(
                $"No .sln file found under the copied asset at '{destination}'.");
        }

        return new TwinCatProjectFixture(
            new DirectoryInfo(destination),
            new FileInfo(solutions[0]));
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (WorkingDirectory.Exists)
            {
                WorkingDirectory.Delete(recursive: true);
            }
        }
        catch
        {
            // best effort
        }

        return ValueTask.CompletedTask;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);

        foreach (string dir in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dir.Replace(source, destination));
        }

        foreach (string file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            File.Copy(file, file.Replace(source, destination), overwrite: true);
        }
    }
}
