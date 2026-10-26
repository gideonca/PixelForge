using PixelForge.Engine.Meshing;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>Uploads a <see cref="ChunkMesh"/> (position-only vertices, attribute location 0) to the GPU.</summary>
public sealed unsafe class GpuMesh : IDisposable
{
    private readonly GL _gl;
    private readonly uint _indexCount;
    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _indexBuffer;

    public GpuMesh(GL gl, ChunkMesh mesh)
    {
        _gl = gl;
        _indexCount = (uint)mesh.Indices.Length;

        _vertexArray = gl.GenVertexArray();
        _vertexBuffer = gl.GenBuffer();
        _indexBuffer = gl.GenBuffer();

        gl.BindVertexArray(_vertexArray);

        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, mesh.Positions, BufferUsageARB.StaticDraw);

        gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _indexBuffer);
        gl.BufferData(BufferTargetARB.ElementArrayBuffer, mesh.Indices, BufferUsageARB.StaticDraw);

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);

        gl.BindVertexArray(0);
    }

    public void Draw()
    {
        _gl.BindVertexArray(_vertexArray);
        _gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    public void Dispose()
    {
        if (_vertexArray == 0)
            return;
        _gl.DeleteBuffer(_vertexBuffer);
        _gl.DeleteBuffer(_indexBuffer);
        _gl.DeleteVertexArray(_vertexArray);
        _vertexArray = _vertexBuffer = _indexBuffer = 0;
    }
}
