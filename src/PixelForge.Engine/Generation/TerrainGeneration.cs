using PixelForge.Engine.World;

namespace PixelForge.Engine.Generation;

public static class TerrainGenerator
{
    /// <summary>Creates a chunk with every block filled with dirt.</summary>
    public static Chunk GenerateSolidChunk()
    {
        var chunk = new Chunk();
        // Fill every coordinate so the returned chunk is completely solid.
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Dirt);
                }
            }
        }
        return chunk;
    }

    /// <summary>Creates an empty chunk by setting every block to air.</summary>
    public static Chunk GenerateChunk()
    {
        var chunk = new Chunk();
        // Explicitly initialize every cell as air for a predictable empty chunk.
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
            }
        }
        return chunk;
    }
}
