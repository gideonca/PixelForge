using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Sandbox;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Debug)
        .AddSimpleConsole(options =>
        {
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
        });
});

using var game = new SandboxGame(
    loggerFactory.CreateLogger<Game>());

game.Run();