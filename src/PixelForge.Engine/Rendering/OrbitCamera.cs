using System.Numerics;

namespace PixelForge.Engine.Rendering;

/// <summary>Builds a view-projection matrix for a camera orbiting a target point.</summary>
public sealed class OrbitCamera
{
    /// <summary>Gets or sets the world-space point the camera looks at.</summary>
    public Vector3 Target { get; set; }

    /// <summary>Gets or sets the camera's distance from <see cref="Target"/>.</summary>
    public float Distance { get; set; } = 34f;

    /// <summary>Gets or sets the horizontal orbit angle in radians.</summary>
    public float Yaw { get; set; }

    /// <summary>Gets or sets the vertical orbit angle in radians.</summary>
    public float Pitch { get; set; } = 0.55f;

    /// <summary>Gets or sets the vertical field of view in radians.</summary>
    public float FieldOfView { get; set; } = 0.6f;

    /// <summary>Gets or sets the viewport width divided by its height.</summary>
    public float AspectRatio { get; set; } = 1f;

    /// <summary>Gets or sets the closest visible depth.</summary>
    public float NearPlane { get; set; } = 0.1f;

    /// <summary>Gets or sets the farthest visible depth.</summary>
    public float FarPlane { get; set; } = 200f;

    /// <summary>Combines the orbit camera's view and perspective projection matrices.</summary>
    public Matrix4x4 ViewProjection
    {
        get
        {
            // Rotate the positive-Z offset around the target to place the camera on its orbit.
            var offset = Vector3.Transform(
                new Vector3(0, 0, Distance),
                Quaternion.CreateFromYawPitchRoll(Yaw, -Pitch, 0));
            var view = Matrix4x4.CreateLookAt(Target + offset, Target, Vector3.UnitY);
            // Row-vector transforms apply the view first, then the projection.
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView, AspectRatio, NearPlane, FarPlane);
            return view * projection;
        }
    }
}
