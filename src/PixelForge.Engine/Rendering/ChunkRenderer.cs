using System.Numerics;
using PixelForge.Engine.Meshing;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>Draws chunk meshes and optional debug block outlines from a camera.</summary>
public sealed class ChunkRenderer : IDisposable
{
    // The solid pass shades block faces using their local position.
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

    // The outline pass uses a separate shader so its color does not affect the solid surfaces.
    private const string EdgeVertexSource = """
        #version 330 core
        layout (location = 0) in vec3 aPosition;
        uniform mat4 uViewProjection;
        uniform mat4 uModel;

        void main()
        {
            gl_Position = uViewProjection * uModel * vec4(aPosition, 1.0);
        }
        """;

    private const string EdgeFragmentSource = """
        #version 330 core
        out vec4 FragColor;

        void main()
        {
            FragColor = vec4(0.04, 0.05, 0.06, 1.0);
        }
        """;

    private readonly GL _gl;
    private readonly ShaderProgram _shader;
    private readonly ShaderProgram _edgeShader;
    private readonly List<(GpuMesh Mesh, Matrix4x4 Model)> _chunks = new();

    /// <summary>Gets or sets whether per-block outlines are drawn for debugging.</summary>
    public bool DebugShowBlockBorders { get; set; } = true;

    /// <summary>Creates the solid-surface and block-outline shader programs.</summary>
    public ChunkRenderer(GL gl)
    {
        _gl = gl;
        _shader = new ShaderProgram(gl, VertexSource, FragmentSource);
        _edgeShader = new ShaderProgram(gl, EdgeVertexSource, EdgeFragmentSource);
    }

    /// <summary>Uploads a mesh and associates it with a translation in world space.</summary>
    public GpuMesh Add(ChunkMesh mesh, Vector3 worldOffset = default)
    {
        var gpuMesh = new GpuMesh(_gl, mesh);
        _chunks.Add((gpuMesh, Matrix4x4.CreateTranslation(worldOffset)));
        return gpuMesh;
    }

    /// <summary>Draws all registered chunks, followed by their optional debug outlines.</summary>
    public void Render(OrbitCamera camera)
    {
        _shader.Use();
        _shader.SetUniform("uViewProjection", camera.ViewProjection);
        foreach (var (mesh, model) in _chunks)
        {
            _shader.SetUniform("uModel", model);
            mesh.Draw();
        }

        // Draw borders after surfaces; Lequal lets coplanar edge fragments pass the depth test.
        if (DebugShowBlockBorders)
        {
            _edgeShader.Use();
            _edgeShader.SetUniform("uViewProjection", camera.ViewProjection);
            _gl.DepthFunc(DepthFunction.Lequal);
            foreach (var (mesh, model) in _chunks)
            {
                _edgeShader.SetUniform("uModel", model);
                mesh.DrawBlockEdges();
            }
            // Restore the default strict comparison for later rendering passes.
            _gl.DepthFunc(DepthFunction.Less);
        }
    }

    /// <summary>Releases every uploaded chunk mesh and both shader programs.</summary>
    public void Dispose()
    {
        foreach (var (mesh, _) in _chunks)
            mesh.Dispose();
        _chunks.Clear();
        _shader.Dispose();
        _edgeShader.Dispose();
    }
}
