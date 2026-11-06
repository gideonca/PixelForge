using System.Numerics;

namespace PixelForge.Engine.Rendering;

/// <summary>A free camera that is moved and rotated directly instead of orbiting a target.</summary>
public sealed class FlyCamera
{
    private const float MoveSpeed = 20f;
    private const float FastMultiplier = 3f;
    private const float LookSensitivity = 0.003f;
    private const float MaxPitch = 1.553f; // Just under 90 degrees, to avoid flipping at the poles.

    private float _pitch;

    /// <summary>Gets or sets the camera's world-space position.</summary>
    public Vector3 Position { get; set; }

    /// <summary>Gets or sets the horizontal angle in radians. Zero looks toward negative Y.</summary>
    public float Yaw { get; set; }

    /// <summary>Gets or sets the vertical angle in radians, clamped to avoid looking straight up or down.</summary>
    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, -MaxPitch, MaxPitch);
    }

    /// <summary>Gets or sets the vertical field of view in radians.</summary>
    public float FieldOfView { get; set; } = 0.9f;

    /// <summary>Gets or sets the viewport width divided by its height.</summary>
    public float AspectRatio { get; set; } = 1f;

    /// <summary>Gets or sets the closest visible depth.</summary>
    public float NearPlane { get; set; } = 0.1f;

    /// <summary>Gets or sets the farthest visible depth.</summary>
    public float FarPlane { get; set; } = 500f;

    /// <summary>Gets the unit vector the camera is looking along.</summary>
    public Vector3 Forward => new(
        MathF.Cos(Pitch) * MathF.Sin(Yaw),
        -MathF.Cos(Pitch) * MathF.Cos(Yaw),
        MathF.Sin(Pitch));

    /// <summary>Gets the unit vector pointing to the camera's screen-right, level with the ground.</summary>
    public Vector3 Right => new(-MathF.Cos(Yaw), -MathF.Sin(Yaw), 0);

    /// <summary>Moves in the given direction, normalizing it to keep diagonal movement consistent.</summary>
    public void Move(Vector3 direction, float elapsedSeconds, bool moveFast = false)
    {
        if (direction == Vector3.Zero)
            return;

        float speed = moveFast ? MoveSpeed * FastMultiplier : MoveSpeed;
        Position += Vector3.Normalize(direction) * speed * elapsedSeconds;
    }

    /// <summary>Applies mouse-look movement to the camera's yaw and pitch.</summary>
    public void Look(Vector2 mouseDelta)
    {
        Yaw -= mouseDelta.X * LookSensitivity;
        Pitch -= mouseDelta.Y * LookSensitivity;
    }

    /// <summary>Combines the camera's view and perspective projection matrices.</summary>
    public Matrix4x4 ViewProjection
    {
        get
        {
            var view = Matrix4x4.CreateLookAt(Position, Position + Forward, Vector3.UnitZ);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView, AspectRatio, NearPlane, FarPlane);
            return view * projection;
        }
    }
}
