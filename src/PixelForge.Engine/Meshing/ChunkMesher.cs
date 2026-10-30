using System.Numerics;
using PixelForge.Engine.World;

namespace PixelForge.Engine.Meshing;

/// <summary>Converts a block grid into visible face triangles and per-block outline edges.</summary>
public static class ChunkMesher
{
    // Each neighbor direction matches the face at the same index.
    private static readonly (int X, int Y, int Z)[] Neighbors =
    [
        (-1,  0,  0), // Left
        ( 1,  0,  0), // Right
        ( 0, -1,  0), // Bottom
        ( 0,  1,  0), // Top
        ( 0,  0, -1), // Negative Z
        ( 0,  0,  1)  // Positive Z
    ];

    // Corners are counterclockwise when viewed from outside.
    private static readonly Vector3[][] FaceCorners =
    [
        // Left
        [
            new(0, 0, 0), new(0, 0, 1),
            new(0, 1, 1), new(0, 1, 0)
        ],

        // Right
        [
            new(1, 0, 1), new(1, 0, 0),
            new(1, 1, 0), new(1, 1, 1)
        ],

        // Bottom
        [
            new(0, 0, 0), new(1, 0, 0),
            new(1, 0, 1), new(0, 0, 1)
        ],

        // Top
        [
            new(0, 1, 0), new(0, 1, 1),
            new(1, 1, 1), new(1, 1, 0)
        ],

        // Negative Z
        [
            new(1, 0, 0), new(0, 0, 0),
            new(0, 1, 0), new(1, 1, 0)
        ],

        // Positive Z
        [
            new(0, 0, 1), new(1, 0, 1),
            new(1, 1, 1), new(0, 1, 1)
        ]
    ];

    // Endpoint indices refer to BlockCorners and enumerate the 12 edges of a unit cube.
    private static readonly (int First, int Second)[] BlockEdges =
    [
        (0, 1), (1, 2), (2, 3), (3, 0),
        (4, 5), (5, 6), (6, 7), (7, 4),
        (0, 4), (1, 5), (2, 6), (3, 7)
    ];

    // Corner order is shared with the endpoint pairs in BlockEdges.
    private static readonly Vector3[] BlockCorners =
    [
        new(0, 0, 0), new(1, 0, 0), new(1, 1, 0), new(0, 1, 0),
        new(0, 0, 1), new(1, 0, 1), new(1, 1, 1), new(0, 1, 1)
    ];

    /// <summary>Builds renderable geometry for every non-air block in a chunk.</summary>
    public static ChunkMesh Build(Chunk chunk)
    {
        ArgumentNullException.ThrowIfNull(chunk);

        var positions = new List<Vector3>();
        var indices = new List<uint>();
        var blockEdges = new List<Vector3>();

        // Visit each cell once; air has no geometry, and solid cells contribute outlines
        // plus only those faces that are exposed to air or outside the chunk.
        for (int z = 0; z < Chunk.ChunkSize; z++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int x = 0; x < Chunk.ChunkSize; x++)
                {
                    if (chunk.GetBlock(x, y, z) == BlockType.Air)
                        continue;

                    var blockPosition = new Vector3(x, y, z);
                    AddBlockEdges(blockPosition, blockEdges);

                    for (int face = 0; face < Neighbors.Length; face++)
                    {
                        var neighbor = Neighbors[face];

                        if (IsSolid(
                            chunk,
                            x + neighbor.X,
                            y + neighbor.Y,
                            z + neighbor.Z))
                        {
                            continue;
                        }

                        AddFace(
                            blockPosition,
                            face,
                            positions,
                            indices);
                    }
                }
            }
        }

        return new ChunkMesh(
            positions.ToArray(),
            indices.ToArray())
        {
            BlockEdges = blockEdges.ToArray()
        };
    }

    private static void AddBlockEdges(Vector3 blockPosition, List<Vector3> positions)
    {
        // Store endpoints in pairs so OpenGL can draw the outline buffer as independent lines.
        foreach (var (first, second) in BlockEdges)
        {
            positions.Add(blockPosition + BlockCorners[first]);
            positions.Add(blockPosition + BlockCorners[second]);
        }
    }

    private static bool IsSolid(Chunk chunk, int x, int y, int z)
    {
        // For now, this is an isolated chunk.
        // Anything outside it is treated as air.
        if (x < 0 || x >= Chunk.ChunkSize ||
            y < 0 || y >= Chunk.ChunkSize ||
            z < 0 || z >= Chunk.ChunkSize)
        {
            return false;
        }

        return chunk.GetBlock(x, y, z) != BlockType.Air;
    }

    // Add a quad as four unique vertices and two indexed triangles.
    private static void AddFace(
        Vector3 blockPosition,
        int face,
        List<Vector3> positions,
        List<uint> indices)
    {
        uint firstVertex = (uint)positions.Count;

        foreach (Vector3 corner in FaceCorners[face])
        {
            positions.Add(blockPosition + corner);
        }

        // First triangle.
        indices.Add(firstVertex);
        indices.Add(firstVertex + 1);
        indices.Add(firstVertex + 2);

        // Second triangle.
        indices.Add(firstVertex);
        indices.Add(firstVertex + 2);
        indices.Add(firstVertex + 3);
    }
}