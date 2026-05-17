namespace TcBuilder.Tests.TestAssets;

/// <summary>
/// Tests fixture to assemble a TwinCAT solution for test.
/// 
/// For each usage it does the following:
/// - Copy an entire solution folder to some writable temp-directory
/// - Remove that folder when finished
/// 
/// This allows us to build, clean, rebuild, etc. in tests without affecting other tests.
/// </summary>
public class SolutionFixture : IDisposable
{
    public SolutionFixture()
    {
        // empty
    }

    public void Dispose()
    {
        // empty
    }
}
