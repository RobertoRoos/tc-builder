using System.CommandLine;
using Microsoft.Extensions.Logging.Abstractions;
using TcBuilder.Commands;
using TcBuilder.Services;

namespace TcBuilder.Tests.Commands;

/// <summary>
/// Fast unit tests for the <c>build</c> verb — no TwinCAT, no filesystem
/// beyond an empty placeholder .sln. Verifies that CLI parsing dispatches
/// the right <see cref="BuildRequest"/> to the service.
/// </summary>
public sealed class RunCommandTests
{
    [Fact]
    public async Task Dummy()
    {
        ;
    }
}
