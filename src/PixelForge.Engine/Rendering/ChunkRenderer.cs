using System.Numerics;
using PixelForge.Engine.Meshing;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>Draws any number of chunk meshes, each at its own world offset, from a camera.</summary>
public sealed class ChunkRenderer : IDisposable
{
    private const string VertexSource = """
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        uniform mat4 uViewProjection;
        uniform mat4 uModel;
        out vec3 vColor;

        void main()
        {
            gl_Position = uViewProjection * uModel * vec4(aPosition, 1.0);
            vColor = vec3(
                0.24 + aPosition.y * 0.018,
                0.48 + aPosition.y * 0.018,
                0.18 + aPosition.x * 0.012);
        }
        """;

    private const string FragmentSource = """
        #version 330 core
        in vec3 vColor;
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(vColor, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly List<(GpuMesh Mesh, Matrix4x4 Model)> _chunks = new();

    public ChunkRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, VertexSource, FragmentSource);
    }

    public GpuMesh Add(ChunkMesh mesh, Vector3 worldOffset = default)
    {
        var gpuMesh = new GpuMesh(_gl, mesh);
        _chunks.Add((gpuMesh, Matrix4x4.CreateTranslation(worldOffset)));
        return gpuMesh;
    }

    public void Render(OrbitCamera camera)
    {
        _shader.Use();
        _shader.SetUniform("uViewProjection", camera.ViewProjection);
        foreach (var (mesh, model) in _chunks)
        {
            _shader.SetUniform("uModel", model);
            mesh.Draw();
        }
    }

    public void Dispose()
    {
        foreach (var (mesh, _) in _chunks)
            mesh.Dispose();
        _chunks.Clear();
        _shader.Dispose();
    }
}
