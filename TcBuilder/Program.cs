using System.CommandLine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TcBuilder.Commands;
using TcBuilder.Services;

// --------------------------------------------------------------------------
//  tcbuilder — CLI for building TwinCAT solutions.
//
//  Architecture:
//    * Microsoft.Extensions.Hosting provides DI, configuration, and logging.
//    * Each CLI verb lives in TcBuilder.Commands.* as a Command subclass
//      and pulls its dependencies through constructor injection.
//    * TwinCAT automation is isolated behind ITwinCatBuildService so the
//      command layer stays thin and testable.
// --------------------------------------------------------------------------

HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

// CLI-friendly console logging: single line, no category spam.
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

// -- Services -------------------------------------------------------------
builder.Services.AddSingleton<ITwinCatBuildService, TwinCatBuildService>();

// -- Commands -------------------------------------------------------------
builder.Services.AddSingleton<RunCommand>();
builder.Services.AddSingleton<BuildCommand>();
builder.Services.AddSingleton<CleanCommand>();
builder.Services.AddSingleton<InfoCommand>();

builder.Services.AddSingleton<RootCommand>(sp =>
{
    RootCommand root = new("tcbuilder — CLI for building TwinCAT solutions.");
    root.Subcommands.Add(sp.GetRequiredService<RunCommand>());
    root.Subcommands.Add(sp.GetRequiredService<BuildCommand>());
    root.Subcommands.Add(sp.GetRequiredService<CleanCommand>());
    root.Subcommands.Add(sp.GetRequiredService<InfoCommand>());
    return root;
});

using IHost host = builder.Build();

RootCommand rootCommand = host.Services.GetRequiredService<RootCommand>();
return await rootCommand.Parse(args).InvokeAsync();
