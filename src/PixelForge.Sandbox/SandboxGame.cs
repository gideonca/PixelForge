using PixelForge.Engine.Generation;
using PixelForge.Engine.Meshing;
using PixelForge.Engine.Core;
using Microsoft.Extensions.Logging;
using Silk.NET.OpenGL;

namespace PixelForge.Sandbox;

public sealed unsafe class SandboxGame : Game
{
    private readonly ChunkMesh _mesh;
    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _indexBuffer;
    private uint _shaderProgram;
    private int _angleLocation;
    private float _angle;

    public SandboxGame(ILogger<Game> logger) 
        : base("PixelForge Sandbox", 1200, 1000, logger)
    {
        var chunk = TerrainGenerator.GenerateSolidChunk();
        _mesh = ChunkMesher.Build(chunk);
    }

    protected override void OnLoad()
    {
        Gl.ClearColor(0.08f, 0.11f, 0.16f, 1f);
        _shaderProgram = CreateShaderProgram();
        _angleLocation = Gl.GetUniformLocation(_shaderProgram, "uAngle");

        _vertexArray = Gl.GenVertexArray();
        _vertexBuffer = Gl.GenBuffer();
        _indexBuffer = Gl.GenBuffer();

        Gl.BindVertexArray(_vertexArray);

        Gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        Gl.BufferData(
            BufferTargetARB.ArrayBuffer,
            _mesh.Positions,
            BufferUsageARB.StaticDraw);

        Gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        Gl.BufferData(
            BufferTargetARB.ElementArrayBuffer,
            _mesh.Indices,
            BufferUsageARB.StaticDraw);

        Gl.EnableVertexAttribArray(0);
        Gl.VertexAttribPointer(
            0,
            3,
            VertexAttribPointerType.Float,
            false,
            3 * sizeof(float),
            (void*)0);

        Gl.BindVertexArray(0);
    }

    protected override void OnRender(double delta)
    {
        _angle += (float)delta * 0.5f;

        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        Gl.UseProgram(_shaderProgram);
        Gl.Uniform1(_angleLocation, _angle);
        Gl.BindVertexArray(_vertexArray);
        Gl.DrawElements(
            PrimitiveType.Triangles,
            (uint)_mesh.Indices.Length,
            DrawElementsType.UnsignedInt,
            (void*)0);
    }

    protected override void OnUnload()
    {
        if (_vertexBuffer != 0)
            Gl.DeleteBuffer(_vertexBuffer);
        if (_indexBuffer != 0)
            Gl.DeleteBuffer(_indexBuffer);
        if (_vertexArray != 0)
            Gl.DeleteVertexArray(_vertexArray);
        if (_shaderProgram != 0)
            Gl.DeleteProgram(_shaderProgram);
    }

    private uint CreateShaderProgram()
    {
        const string vertexSource = """
            #version 330 core
            layout (location = 0) in vec3 aPosition;
            uniform float uAngle;
            out vec3 vColor;

            void main()
            {
                vec3 p = aPosition - vec3(8.0);

                float c = cos(uAngle);
                float s = sin(uAngle);
                p = vec3(c * p.x + s * p.z, p.y, -s * p.x + c * p.z);

                float tilt = 0.55;
                float ct = cos(tilt);
                float st = sin(tilt);
                p = vec3(p.x, ct * p.y - st * p.z, st * p.y + ct * p.z);

                float depth = 34.0 - p.z;
                gl_Position = vec4(
                    p.x * 1.7,
                    p.y * 1.7,
                    1.002002 * depth - 0.2002,
                    depth);

                vColor = vec3(
                    0.24 + aPosition.y * 0.018,
                    0.48 + aPosition.y * 0.018,
                    0.18 + aPosition.x * 0.012);
            }
            """;
        const string fragmentSource = """
            #version 330 core
            in vec3 vColor;
            out vec4 FragColor;

            void main()
            {
                FragColor = vec4(vColor, 1.0);
            }
            """;

        uint vertexShader = 0;
        uint fragmentShader = 0;
        uint program = 0;

        try
        {
            vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
            fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);
            program = Gl.CreateProgram();

            Gl.AttachShader(program, vertexShader);
            Gl.AttachShader(program, fragmentShader);
            Gl.LinkProgram(program);

            if (Gl.GetProgram(program, ProgramPropertyARB.LinkStatus) == 0)
                throw new InvalidOperationException(
                    $"Failed to link chunk shader program: {Gl.GetProgramInfoLog(program)}");

            return program;
        }
        catch
        {
            if (program != 0)
                Gl.DeleteProgram(program);
            throw;
        }
        finally
        {
            if (vertexShader != 0)
                Gl.DeleteShader(vertexShader);
            if (fragmentShader != 0)
                Gl.DeleteShader(fragmentShader);
        }
    }

    private uint CompileShader(ShaderType type, string source)
    {
        uint shader = Gl.CreateShader(type);
        Gl.ShaderSource(shader, source);
        Gl.CompileShader(shader);

        if (Gl.GetShader(shader, ShaderParameterName.CompileStatus) != 0)
            return shader;

        string log = Gl.GetShaderInfoLog(shader);
        Gl.DeleteShader(shader);
        throw new InvalidOperationException($"Failed to compile {type} shader: {log}");
    }
}
