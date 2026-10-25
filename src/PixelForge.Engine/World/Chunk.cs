namespace PixelForge.Engine.World;

public sealed class Chunk
{
    public const int ChunkSize = 16;

    private readonly BlockType[] _blocks = 
        new BlockType[ChunkSize * ChunkSize * ChunkSize];

    public BlockType GetBlock(int x, int y, int z) => 
        _blocks[Index(x, y, z)];

    public void SetBlock(int x, int y, int z, BlockType block) => 
        _blocks[Index(x, y, z)] = block;

    private static int Index(int x, int y, int z)
    {
        switch (x)
        {
            case < 0 or >= ChunkSize:
                throw new ArgumentOutOfRangeException(nameof(x));
        }
        switch (y)
        {
            case < 0 or >= ChunkSize:
                throw new ArgumentOutOfRangeException(nameof(y));
        }
        switch (z)
        {
            case < 0 or >= ChunkSize:
                throw new ArgumentOutOfRangeException(nameof(z));
        }

        return x + ChunkSize * (y + ChunkSize * z);
    }
}