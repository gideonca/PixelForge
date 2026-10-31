namespace PixelForge.Engine.World;

/// <summary>A fixed-size, three-dimensional grid of block types.</summary>
public sealed class Chunk
{
    /// <summary>The number of block cells along each axis.</summary>
    public const int ChunkSize = 16;

    // Blocks are stored in z-major slices, with x varying fastest.
    private readonly BlockType[] _blocks = 
        new BlockType[ChunkSize * ChunkSize * ChunkSize];
    private readonly float[] _terrainNoise = new float[ChunkSize * ChunkSize];

    /// <summary>Returns the block at the given local chunk coordinates.</summary>
    public BlockType GetBlock(int x, int y, int z) => 
        _blocks[Index(x, y, z)];

    /// <summary>Sets the block at the given local chunk coordinates.</summary>
    public void SetBlock(int x, int y, int z, BlockType block) => 
        _blocks[Index(x, y, z)] = block;

    internal float GetTerrainNoise(int x, int z) =>
        _terrainNoise[x + ChunkSize * z];

    internal void SetTerrainNoise(int x, int z, float noise) =>
        _terrainNoise[x + ChunkSize * z] = noise;

    private static int Index(int x, int y, int z)
    {
        // Check each axis separately so callers get an exception naming the invalid coordinate.
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

        // Flatten the 3D coordinate into the backing array's linear index.
        return x + ChunkSize * (y + ChunkSize * z);
    }
}