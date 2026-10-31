using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using PixelForge.Engine.World;

namespace PixelForge.Engine.Generation;

public static class TerrainGenerator
{
    private const float BaseTerrainHeight = 7.5f;
    private const float TerrainHeightVariation = 5f;
    private const float BaseNoiseScale = 0.08f;
    private const int OctaveCount = 4;

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

    /// <summary>
    /// Creates a terrain chunk whose height map is determined by a 32-character
    /// decimal or hexadecimal seed.
    /// </summary>
    public static Chunk GenerateTerrainChunk(string seed, int chunkX = 0, int chunkZ = 0)
    {
        ArgumentNullException.ThrowIfNull(seed);
        if (seed.Length != 32 || !seed.All(Uri.IsHexDigit))
        {
            throw new ArgumentException(
                "The seed must contain exactly 32 decimal or hexadecimal digits.",
                nameof(seed));
        }

        byte[] seedHash = SHA256.HashData(Encoding.ASCII.GetBytes(seed.ToUpperInvariant()));
        ulong seedLow = BinaryPrimitives.ReadUInt64LittleEndian(seedHash);
        ulong seedHigh = BinaryPrimitives.ReadUInt64LittleEndian(seedHash.AsSpan(sizeof(ulong)));
        var chunk = new Chunk();

        for (int z = 0; z < Chunk.ChunkSize; z++)
        {
            for (int x = 0; x < Chunk.ChunkSize; x++)
            {
                float worldX = ((float)chunkX * Chunk.ChunkSize + x) * BaseNoiseScale;
                float worldZ = ((float)chunkZ * Chunk.ChunkSize + z) * BaseNoiseScale;
                float noise = FractalNoise(worldX, worldZ, seedLow, seedHigh);
                chunk.SetTerrainNoise(x, z, noise);
                int height = Math.Clamp(
                    (int)MathF.Round(BaseTerrainHeight + noise * TerrainHeightVariation),
                    1,
                    Chunk.ChunkSize);

                for (int y = 0; y < height; y++)
                {
                    BlockType block = y == height - 1
                        ? BlockType.Grass
                        : y >= height - 4
                            ? BlockType.Dirt
                            : BlockType.Stone;
                    chunk.SetBlock(x, y, z, block);
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

    private static float FractalNoise(float x, float z, ulong seedLow, ulong seedHigh)
    {
        float total = 0;
        float amplitude = 1;
        float amplitudeTotal = 0;

        for (int octave = 0; octave < OctaveCount; octave++)
        {
            total += ValueNoise(x, z, seedLow, seedHigh, octave) * amplitude;
            amplitudeTotal += amplitude;
            x *= 2;
            z *= 2;
            amplitude *= 0.5f;
        }

        return total / amplitudeTotal;
    }

    private static float ValueNoise(float x, float z, ulong seedLow, ulong seedHigh, int octave)
    {
        int x0 = (int)MathF.Floor(x);
        int z0 = (int)MathF.Floor(z);
        float tx = Fade(x - x0);
        float tz = Fade(z - z0);

        float top = Lerp(
            LatticeValue(x0, z0, seedLow, seedHigh, octave),
            LatticeValue(x0 + 1, z0, seedLow, seedHigh, octave),
            tx);
        float bottom = Lerp(
            LatticeValue(x0, z0 + 1, seedLow, seedHigh, octave),
            LatticeValue(x0 + 1, z0 + 1, seedLow, seedHigh, octave),
            tx);
        return Lerp(top, bottom, tz);
    }

    private static float LatticeValue(int x, int z, ulong seedLow, ulong seedHigh, int octave)
    {
        ulong value = seedLow
            ^ ((ulong)(uint)x * 0x9E3779B185EBCA87UL)
            ^ ((ulong)(uint)z * 0xC2B2AE3D27D4EB4FUL)
            ^ ((ulong)(uint)octave * 0x165667B19E3779F9UL)
            ^ seedHigh;
        value ^= value >> 30;
        value *= 0xBF58476D1CE4E5B9UL;
        value ^= value >> 27;
        value *= 0x94D049BB133111EBUL;
        value ^= value >> 31;
        return (value >> 40) / (float)0xFFFFFF * 2f - 1f;
    }

    private static float Fade(float value) =>
        value * value * value * (value * (value * 6f - 15f) + 10f);

    private static float Lerp(float first, float second, float amount) =>
        first + (second - first) * amount;
}
