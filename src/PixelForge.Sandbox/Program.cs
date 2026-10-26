using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
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

var chunk = TerrainGenerator.GenerateSolidChunk();
var mesh = ChunkMesher.Build(chunk);

Console.WriteLine($"Vertices: {mesh.Positions.Length}");
Console.WriteLine($"Triangles: {mesh.Indices.Length / 3}");

game.Run();