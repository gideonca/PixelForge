using System.Numerics;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

public sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniforms = new();
    private uint _handle;

    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        _handle = Link(vertexSource, fragmentSource);
    }

    public void Use() => _gl.UseProgram(_handle);

    public void SetUniform(string name, float value) =>
        _gl.Uniform1(Location(name), value);

    public unsafe void SetUniform(string name, in Matrix4x4 value)
    {
        // System.Numerics is row-major; GLSL reads the same memory as the transpose, which matches row-vector math.
        fixed (float* p = &value.M11)
            _gl.UniformMatrix4(Location(name), 1, false, p);
    }

    private int Location(string name)
    {
        if (!_uniforms.TryGetValue(name, out int location))
            _uniforms[name] = location = _gl.GetUniformLocation(_handle, name);
        return location;
    }

    private uint Link(string vertexSource, string fragmentSource)
    {
        uint vertexShader = 0;
        uint fragmentShader = 0;
        uint program = 0;

        try
        {
            vertexShader = Compile(ShaderType.VertexShader, vertexSource);
            fragmentShader = Compile(ShaderType.FragmentShader, fragmentSource);
            program = _gl.CreateProgram();

            _gl.AttachShader(program, vertexShader);
            _gl.AttachShader(program, fragmentShader);
            _gl.LinkProgram(program);

            if (_gl.GetProgram(program, ProgramPropertyARB.LinkStatus) == 0)
                throw new InvalidOperationException(
                    $"Failed to link shader program: {_gl.GetProgramInfoLog(program)}");

            return program;
        }
        catch
        {
            if (program != 0)
                _gl.DeleteProgram(program);
            throw;
        }
        finally
        {
            if (vertexShader != 0)
                _gl.DeleteShader(vertexShader);
            if (fragmentShader != 0)
                _gl.DeleteShader(fragmentShader);
        }
    }

    private uint Compile(ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        if (_gl.GetShader(shader, ShaderParameterName.CompileStatus) != 0)
            return shader;

        string log = _gl.GetShaderInfoLog(shader);
        _gl.DeleteShader(shader);
        throw new InvalidOperationException($"Failed to compile {type} shader: {log}");
    }

    public void Dispose()
    {
        if (_handle == 0)
            return;
        _gl.DeleteProgram(_handle);
        _handle = 0;
    }
}
