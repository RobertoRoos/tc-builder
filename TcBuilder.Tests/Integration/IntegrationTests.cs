using TcBuilder.Tests.TestAssets;

namespace TcBuilder.Tests.Integration;

/// <summary>
/// End-to-end tests that exercise the real <see cref="TcService"/>.
/// 
/// This is not a true unit-test testcase, it is full integration test. It
/// depends on a real TwinCAT IDE being present and it will perform real TwinCAT builds.
/// </summary>
[Trait("Category", "Integration")]
public sealed class IntegrationTests : IDisposable
{
    SolutionFixture? solution = null;

    /// <summary>
    /// Constructor
    /// </summary>
    public IntegrationTests()
    {
        // empty
    }

    /// <summary>
    /// Finalizer (deconstructor)
    /// </summary>
    public void Dispose()
    {
        if (solution is not null)
        {
            solution.Dispose();
            solution = null;
        }
    }

    [Fact]
    public void Some_Test_Method()
    {
        int x = 1;
        x.ShouldBe(x);
    }
}
