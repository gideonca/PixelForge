using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Rendering;

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

using var game = new ChunkPreviewGame(
    loggerFactory.CreateLogger<Game>());

game.Run();