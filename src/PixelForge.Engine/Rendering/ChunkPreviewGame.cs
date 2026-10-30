using System.Numerics;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.World;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>A sample game that generates, uploads, and rotates a chunk preview.</summary>
public sealed class ChunkPreviewGame : Game
{
    private readonly ChunkMesh _mesh;
    private readonly int _chunkCount;
    private readonly int _columns;
    private readonly OrbitCamera _camera;
    private ChunkRenderer? _renderer;

    /// <summary>
    /// Creates a preview of <paramref name="chunkCount"/> chunks, each using the supplied mesh
    /// or a generated solid chunk, arranged in a grid on the XZ plane.
    /// </summary>
    public ChunkPreviewGame(
        ILogger<Game> logger,
        ChunkMesh? mesh = null,
        int chunkCount = 4,
        string title = "PixelForge Sandbox",
        int width = 1200,
        int height = 1000)
        : base(title, width, height, logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkCount, 1);
        _chunkCount = chunkCount;
        // A square-ish grid keeps the layout compact for any count.
        _columns = (int)Math.Ceiling(Math.Sqrt(chunkCount));
        int rows = (int)Math.Ceiling(chunkCount / (double)_columns);
        float gridExtent = Math.Max(_columns, rows) * Chunk.ChunkSize;

        // Prepare the mesh before the graphics context is created; GPU upload happens in OnLoad.
        _mesh = mesh ?? ChunkMesher.Build(TerrainGenerator.GenerateSolidChunk());
        _camera = new OrbitCamera
        {
            // Aim at the center of the whole grid and pull back to fit it in view.
            Target = new Vector3(_columns * Chunk.ChunkSize / 2f, Chunk.ChunkSize / 2f, rows * Chunk.ChunkSize / 2f),
            Distance = Math.Max(34f, gridExtent * 2f),
            FarPlane = Math.Max(200f, gridExtent * 6f),
            AspectRatio = width / (float)height
        };
    }

    protected override void OnLoad()
    {
        Gl.ClearColor(0.08f, 0.11f, 0.16f, 1f);
        _renderer = new ChunkRenderer(Gl);
        for (int i = 0; i < _chunkCount; i++)
        {
            // Place chunks side by side in row-major order.
            var offset = new Vector3(
                i % _columns * Chunk.ChunkSize,
                0,
                i / _columns * Chunk.ChunkSize);
            _renderer.Add(_mesh, offset);
        }
    }

    protected override void OnKeyDown(Key key)
    {
        // F6 toggles the chunk boundary overlay.
        if (key == Key.F6 && _renderer is not null)
            _renderer.ShowChunkBoundaries = !_renderer.ShowChunkBoundaries;
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
