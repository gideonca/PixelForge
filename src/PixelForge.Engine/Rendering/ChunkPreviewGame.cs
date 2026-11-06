using System.Numerics;
using Microsoft.Extensions.Logging;
using ControlsHandler = PixelForge.Engine.Controls.Controls;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.World;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>
/// A sample game that generates and uploads a chunk preview you can fly around.
/// Controls: W forward, S back, A left, D right, Q/E down/up; Shift is faster.
/// Hold the right mouse button to look; F6 toggles chunk boundaries.
/// </summary>
public sealed class ChunkPreviewGame : Game
{
    private readonly List<(ChunkMesh Mesh, Vector3 Offset)> _chunks = [];

    private readonly FlyCamera _camera;
    private readonly ControlsHandler _controls;
    private ChunkRenderer? _renderer;

    /// <summary>
    /// Creates a preview with <paramref name="chunksPerAxis"/> chunks along each horizontal axis
    /// and one additional chunk below elevation zero, using the supplied mesh or generated terrain.
    /// </summary>
    public ChunkPreviewGame(
        ILogger<Game> logger,
        string seed,
        ChunkMesh? mesh = null,
        int chunksPerAxis = 4,
        string title = "PixelForge Sandbox",
        int width = 1200,
        int height = 1000)
        : base(title, width, height, logger)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(chunksPerAxis, 1);
        float gridExtent = chunksPerAxis * Chunk.ChunkSize;

        // Generate the horizontal chunk grid plus one submerged elevation layer. GPU upload happens later.
        const int firstChunkZ = -1;
        for (int chunkY = 0; chunkY < chunksPerAxis; chunkY++)
        {
            for (int chunkZ = firstChunkZ; chunkZ < chunksPerAxis; chunkZ++)
            {
                for (int chunkX = 0; chunkX < chunksPerAxis; chunkX++)
                {
                    var chunkMesh = mesh ?? ChunkMesher.Build(
                        TerrainGenerator.GenerateTerrainChunk(
                            seed, chunkX, chunkY, chunkZ, chunksPerAxis, chunksPerAxis));
                    var offset = new Vector3(
                        chunkX * Chunk.ChunkSize,
                        chunkY * Chunk.ChunkSize,
                        chunkZ * Chunk.ChunkSize);
                    _chunks.Add((chunkMesh, offset));
                }
            }
        }

        _camera = new FlyCamera
        {
            // Start outside the chunk grid, above its center.
            Position = new Vector3(
                gridExtent / 2f,
                gridExtent * 1.5f,
                gridExtent),
            Pitch = -MathF.Atan(0.5f),
            FarPlane = Math.Max(500f, gridExtent * 6f),
            AspectRatio = width / (float)height
        };
        _controls = new ControlsHandler(_camera);
        _controls.ChunkBoundariesToggleRequested += ToggleChunkBoundaries;
    }

    protected override void OnLoad()
    {
        Gl.ClearColor(0.08f, 0.11f, 0.16f, 1f);
        _renderer = new ChunkRenderer(Gl);
        foreach (var (mesh, offset) in _chunks)
            _renderer.Add(mesh, offset);
    }

    protected override void OnKeyDown(Key key)
    {
        _controls.HandleKeyDown(key);
    }

    protected override void OnMouseMove(Vector2 delta)
    {
        _controls.HandleMouseMove(delta, IsMouseButtonDown(MouseButton.Right));
    }

    protected override void OnUpdate(double delta)
    {
        _controls.Update(IsKeyDown, (float)delta);
    }

    protected override void OnRender(double delta)
    {
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

    private void ToggleChunkBoundaries()
    {
        if (_renderer is not null)
            _renderer.ShowChunkBoundaries = !_renderer.ShowChunkBoundaries;
    }
}
