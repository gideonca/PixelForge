using System.Numerics;

namespace PixelForge.Engine.Rendering;

public sealed class OrbitCamera
{
    public Vector3 Target { get; set; }
    public float Distance { get; set; } = 34f;
    public float Yaw { get; set; }
    public float Pitch { get; set; } = 0.55f;
    public float FieldOfView { get; set; } = 0.6f;
    public float AspectRatio { get; set; } = 1f;
    public float NearPlane { get; set; } = 0.1f;
    public float FarPlane { get; set; } = 200f;

    public Matrix4x4 ViewProjection
    {
        get
        {
            var offset = Vector3.Transform(
                new Vector3(0, 0, Distance),
                Quaternion.CreateFromYawPitchRoll(Yaw, -Pitch, 0));
            var view = Matrix4x4.CreateLookAt(Target + offset, Target, Vector3.UnitY);
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(
                FieldOfView, AspectRatio, NearPlane, FarPlane);
            return view * projection;
        }
    }
}
