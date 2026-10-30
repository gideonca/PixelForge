using System.Numerics;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>Compiles, links, and manages an OpenGL program with cached uniform locations.</summary>
public sealed class ShaderProgram : IDisposable
{
    private readonly GL _gl;
    private readonly Dictionary<string, int> _uniforms = new();
    private uint _handle;

    /// <summary>Creates a program from GLSL vertex and fragment shader source.</summary>
    public ShaderProgram(GL gl, string vertexSource, string fragmentSource)
    {
        _gl = gl;
        _handle = Link(vertexSource, fragmentSource);
    }

    /// <summary>Makes this program active for subsequent draw calls.</summary>
    public void Use() => _gl.UseProgram(_handle);

    /// <summary>Sets a floating-point shader uniform.</summary>
    public void SetUniform(string name, float value) =>
        _gl.Uniform1(Location(name), value);

    /// <summary>Sets a matrix shader uniform using the layout expected by the renderer's shaders.</summary>
    public unsafe void SetUniform(string name, in Matrix4x4 value)
    {
        // System.Numerics is row-major; GLSL reads the same memory as the transpose, which matches row-vector math.
        fixed (float* p = &value.M11)
            _gl.UniformMatrix4(Location(name), 1, false, p);
    }

    private int Location(string name)
    {
        // Uniform lookup is stable for a linked program, so query each name only once.
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
            // Compile both stages before creating and linking the final program.
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
            // Once linked, the program retains the shader code; the shader objects can be deleted.
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
        // Surface compiler diagnostics to the caller rather than returning an unusable program.
        throw new InvalidOperationException($"Failed to compile {type} shader: {log}");
    }

    /// <summary>Deletes the linked program. Safe to call more than once.</summary>
    public void Dispose()
    {
        if (_handle == 0)
            return;
        _gl.DeleteProgram(_handle);
        _handle = 0;
    }
}
