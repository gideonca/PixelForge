using System.Numerics;
using Microsoft.Extensions.Logging;
using PixelForge.Engine.Core;
using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.World;
using Silk.NET.Input;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>
/// A sample game that generates and uploads a chunk preview you can fly around.
/// Controls: W/A/S/D move, Q/E move down/up, Shift is faster, hold the right mouse button to look, F6 toggles chunk boundaries.
/// </summary>
public sealed class ChunkPreviewGame : Game
{
    private readonly ChunkMesh _mesh;
    private readonly int _chunkCount;
    private readonly int _columns;
    private const float MoveSpeed = 20f;
    private const float FastMultiplier = 3f;
    private const float LookSensitivity = 0.003f;

    private readonly FlyCamera _camera;
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
        _camera = new FlyCamera
        {
            // Start outside the grid, centered on it and looking toward it from slightly above.
            Position = new Vector3(
                _columns * Chunk.ChunkSize / 2f,
                Chunk.ChunkSize,
                rows * Chunk.ChunkSize + gridExtent),
            Pitch = -0.25f,
            FarPlane = Math.Max(500f, gridExtent * 6f),
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

    protected override void OnMouseMove(Vector2 delta)
    {
        // Only look around while the right button is held, so the cursor stays free otherwise.
        if (!IsMouseButtonDown(MouseButton.Right))
            return;

        _camera.Yaw += delta.X * LookSensitivity;
        _camera.Pitch -= delta.Y * LookSensitivity;
    }

    protected override void OnUpdate(double delta)
    {
        // Build the movement direction from the held keys.
        var direction = Vector3.Zero;
        if (IsKeyDown(Key.W)) direction += _camera.Forward;
        if (IsKeyDown(Key.S)) direction -= _camera.Forward;
        if (IsKeyDown(Key.D)) direction += _camera.Right;
        if (IsKeyDown(Key.A)) direction -= _camera.Right;
        if (IsKeyDown(Key.E)) direction += Vector3.UnitY;
        if (IsKeyDown(Key.Q)) direction -= Vector3.UnitY;

        if (direction == Vector3.Zero)
            return;

        // Normalize so diagonal movement is not faster, and scale by time for frame-rate independence.
        float speed = IsKeyDown(Key.ShiftLeft) ? MoveSpeed * FastMultiplier : MoveSpeed;
        _camera.Position += Vector3.Normalize(direction) * speed * (float)delta;
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
}
