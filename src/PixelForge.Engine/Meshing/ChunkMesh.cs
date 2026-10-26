using System.Numerics;

namespace PixelForge.Engine.Meshing;

public sealed record ChunkMesh(
    Vector3[] Positions,
    uint[] Indices);