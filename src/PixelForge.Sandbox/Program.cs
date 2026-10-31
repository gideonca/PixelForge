using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Rendering;

if (args.Length != 1)
{
    Console.Error.WriteLine(
        "Usage: dotnet run --project src/PixelForge.Sandbox -- <32-character-seed>");
    return 1;
}

// Configure console logging for the sandbox run.
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

// The sandbox entry point runs the generated chunk preview.
using var game = new ChunkPreviewGame(
        loggerFactory.CreateLogger<Game>(),
        args[0]);

game.Run();
return 0;