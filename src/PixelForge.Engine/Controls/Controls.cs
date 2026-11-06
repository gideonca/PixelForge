using System.Numerics;
using PixelForge.Engine.Rendering;
using Silk.NET.Input;

namespace PixelForge.Engine.Controls;

/// <summary>Maps keyboard and mouse input to camera controls and preview commands.</summary>
public sealed class Controls
{
    private readonly FlyCamera _camera;

    /// <summary>Raised when the chunk-boundary display should be toggled.</summary>
    public event Action? ChunkBoundariesToggleRequested;

    /// <summary>Creates controls that operate the specified camera.</summary>
    public Controls(FlyCamera camera)
    {
        ArgumentNullException.ThrowIfNull(camera);
        _camera = camera;
    }

    /// <summary>Handles a key press that triggers a one-time command.</summary>
    public void HandleKeyDown(Key key)
    {
        if (key == Key.F6)
            ChunkBoundariesToggleRequested?.Invoke();
    }

    /// <summary>Updates camera movement from the currently held keys.</summary>
    public void Update(Func<Key, bool> isKeyDown, float elapsedSeconds)
    {
        ArgumentNullException.ThrowIfNull(isKeyDown);

        var direction = Vector3.Zero;
        if (isKeyDown(Key.W)) direction += _camera.Forward;
        if (isKeyDown(Key.S)) direction -= _camera.Forward;
        if (isKeyDown(Key.D)) direction += _camera.Right;
        if (isKeyDown(Key.A)) direction -= _camera.Right;
        if (isKeyDown(Key.E)) direction += Vector3.UnitZ;
        if (isKeyDown(Key.Q)) direction -= Vector3.UnitZ;

        _camera.Move(direction, elapsedSeconds, isKeyDown(Key.ShiftLeft));
    }

    /// <summary>Applies mouse look when the look button is held.</summary>
    public void HandleMouseMove(Vector2 delta, bool lookButtonDown)
    {
        if (lookButtonDown)
            _camera.Look(delta);
    }
}