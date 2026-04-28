namespace TcBuilder.Tests.Infrastructure;

/// <summary>
/// Creates an empty throwaway <c>.sln</c> file so command-wiring tests can
/// satisfy the "solution must exist" check without a real TwinCAT project.
/// </summary>
internal sealed class TempSolutionFile : IAsyncDisposable
{
    public string Path { get; }

    private TempSolutionFile(string path) => Path = path;

    public static TempSolutionFile Create()
    {
        string path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            $"tcbuilder-tests-{Guid.NewGuid():N}.sln");

        File.WriteAllText(path, "Microsoft Visual Studio Solution File, Format Version 12.00" + Environment.NewLine);
        return new TempSolutionFile(path);
    }

    public ValueTask DisposeAsync()
    {
        try
        {
            if (File.Exists(Path))
            {
                File.Delete(Path);
            }
        }
        catch
        {
            // best effort — tests shouldn't fail because of cleanup
        }

        return ValueTask.CompletedTask;
    }
}
