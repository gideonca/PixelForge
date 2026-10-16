using PixelForge.Engine.Core;
using Microsoft.Extensions.Logging;


namespace PixelForge.Sandbox;

public sealed class SandboxGame : Game
{
    public SandboxGame(ILogger<Game> logger) 
        : base("PixelForge Sandbox", 1200, 1000, logger)
    {
        
    }

    protected override void OnLoad()
    {
        Console.WriteLine("The game window is ready.");
    }

}
