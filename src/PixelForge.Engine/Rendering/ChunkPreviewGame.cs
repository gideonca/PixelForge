using System.Numerics;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.World;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>A sample game that generates, uploads, and rotates a chunk preview.</summary>
public sealed class ChunkPreviewGame : Game
{
    private readonly ChunkMesh _mesh;
    private readonly OrbitCamera _camera;
    private ChunkRenderer? _renderer;

    /// <summary>Creates a preview using the supplied mesh or a generated solid chunk.</summary>
    public ChunkPreviewGame(
        ILogger<Game> logger,
        ChunkMesh? mesh = null,
        string title = "PixelForge Sandbox",
        int width = 1200,
        int height = 1000)
        : base(title, width, height, logger)
    {
        // Prepare the mesh before the graphics context is created; GPU upload happens in OnLoad.
        _mesh = mesh ?? ChunkMesher.Build(TerrainGenerator.GenerateSolidChunk());
        _camera = new OrbitCamera
        {
            Target = new Vector3(Chunk.ChunkSize / 2f),
            AspectRatio = width / (float)height
        };
    }

    protected override void OnLoad()
    {
        Gl.ClearColor(0.08f, 0.11f, 0.16f, 1f);
        _renderer = new ChunkRenderer(Gl);
        // Register the preview mesh at the chunk's origin.
        _renderer.Add(_mesh);
    }

    protected override void OnRender(double delta)
    {
        // Rotate at a fixed angular speed regardless of the frame rate.
        _camera.Yaw += (float)delta * 0.5f;

        // Clear both color and depth before drawing the current frame.
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _renderer?.Render(_camera);
    }

    protected override void OnUnload()
    {
        // GPU resources must be released while the OpenGL context is still available.
        _renderer?.Dispose();
        _renderer = null;
    }
}
