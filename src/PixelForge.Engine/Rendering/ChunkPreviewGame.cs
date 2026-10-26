using System.Numerics;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.World;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

public sealed class ChunkPreviewGame : Game
{
    private readonly ChunkMesh _mesh;
    private readonly OrbitCamera _camera;
    private ChunkRenderer? _renderer;

    public ChunkPreviewGame(
        ILogger<Game> logger,
        ChunkMesh? mesh = null,
        string title = "PixelForge Sandbox",
        int width = 1200,
        int height = 1000)
        : base(title, width, height, logger)
    {
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
        _renderer.Add(_mesh);
    }

    protected override void OnRender(double delta)
    {
        _camera.Yaw += (float)delta * 0.5f;

        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        _renderer?.Render(_camera);
    }

    protected override void OnUnload()
    {
        _renderer?.Dispose();
        _renderer = null;
    }
}
