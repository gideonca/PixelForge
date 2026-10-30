using PixelForge.Engine.Meshing;
using Silk.NET.OpenGL;

namespace PixelForge.Engine.Rendering;

/// <summary>
/// Uploads a <see cref="ChunkMesh"/> to the GPU. Solid vertices use indexed triangles;
/// optional block outlines use line endpoint pairs.
/// </summary>
public sealed unsafe class GpuMesh : IDisposable
{
    private readonly GL _gl;
    private readonly uint _indexCount;
    private readonly uint _edgeVertexCount;
    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _indexBuffer;
    private uint _edgeVertexArray;
    private uint _edgeVertexBuffer;

    /// <summary>Creates GPU buffers and vertex arrays for the solid mesh and its outlines.</summary>
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

        _edgeVertexCount = (uint)mesh.BlockEdges.Length;
        // Meshes without outline data need only the solid vertex array and buffers.
        if (_edgeVertexCount == 0)
            return;

        _edgeVertexArray = gl.GenVertexArray();
        _edgeVertexBuffer = gl.GenBuffer();

        gl.BindVertexArray(_edgeVertexArray);
        gl.BindBuffer(BufferTargetARB.ArrayBuffer, _edgeVertexBuffer);
        gl.BufferData(BufferTargetARB.ArrayBuffer, mesh.BlockEdges, BufferUsageARB.StaticDraw);
        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), (void*)0);
        gl.BindVertexArray(0);
    }

    /// <summary>Draws the solid triangle faces.</summary>
    public void Draw()
    {
        _gl.BindVertexArray(_vertexArray);
        _gl.DrawElements(PrimitiveType.Triangles, _indexCount, DrawElementsType.UnsignedInt, (void*)0);
    }

    /// <summary>Draws each consecutive pair of outline vertices as a line segment.</summary>
    public void DrawBlockEdges()
    {
        if (_edgeVertexCount == 0)
            return;

        _gl.BindVertexArray(_edgeVertexArray);
        _gl.DrawArrays(PrimitiveType.Lines, 0, _edgeVertexCount);
    }

    /// <summary>Deletes all GPU resources owned by this mesh.</summary>
    public void Dispose()
    {
        if (_vertexArray != 0)
        {
            _gl.DeleteBuffer(_vertexBuffer);
            _gl.DeleteBuffer(_indexBuffer);
            _gl.DeleteVertexArray(_vertexArray);
            _vertexArray = _vertexBuffer = _indexBuffer = 0;
        }

        if (_edgeVertexArray != 0)
        {
            _gl.DeleteBuffer(_edgeVertexBuffer);
            _gl.DeleteVertexArray(_edgeVertexArray);
            _edgeVertexArray = _edgeVertexBuffer = 0;
        }
    }
}
