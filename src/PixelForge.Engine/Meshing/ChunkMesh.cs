using System.Numerics;

namespace PixelForge.Engine.Meshing;

/// <summary>CPU-side vertex data for a chunk's solid faces and optional block outlines.</summary>
/// <param name="Positions">Triangle vertex positions, grouped by the indices.</param>
/// <param name="Indices">Indices that form the visible block-face triangles.</param>
public sealed record ChunkMesh(
    Vector3[] Positions,
    uint[] Indices)
{
    /// <summary>
    /// Line endpoint positions for block outlines; each consecutive pair forms one edge.
    /// Empty by default for meshes that do not include debug outlines.
    /// </summary>
    public Vector3[] BlockEdges { get; init; } = [];
}