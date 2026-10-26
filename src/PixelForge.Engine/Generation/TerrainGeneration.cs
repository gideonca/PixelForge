using PixelForge.Engine.World;

namespace PixelForge.Engine.Generation;

public static class TerrainGenerator
{
    public static Chunk GenerateSolidChunk()
    {
        var chunk = new Chunk();
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

    public static Chunk GenerateChunk()
    {
        var chunk = new Chunk();
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
