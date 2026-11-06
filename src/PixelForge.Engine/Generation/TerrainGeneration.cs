// Provides helpers for reading fixed-width integers from the seed hash.
using System.Buffers.Binary;
// Supplies SHA-256 so the text seed can initialize deterministic noise.
using System.Security.Cryptography;
// Converts the validated seed string into bytes before hashing.
using System.Text;
// Gives the generator access to chunks and block material types.
using PixelForge.Engine.World;

// Places terrain-generation code in its own namespace.
namespace PixelForge.Engine.Generation;

// Groups terrain-generation operations that do not need an object instance.
public static class TerrainGenerator
{
    // Sets the average surface as a fraction of the world's total vertical size.
    private const float BaseTerrainHeight = 0.47f;
    // Limits hills and valleys to a gentler fraction of the world's total vertical size.
    private const float TerrainHeightVariation = 0.14f;
    // A lower frequency spreads terrain changes over more world blocks.
    private const float BaseNoiseScale = 0.045f;
    // Each finer noise scale has less influence than the one before it.
    private const float OctavePersistence = 0.35f;
    // Combines four noise scales to create broad shapes and subtle terrain details.
    private const int OctaveCount = 4;
    // Samples cave noise at a scale that creates tunnels spanning several blocks.
    private const float CaveNoiseScale = 0.085f;
    // Limits how far the cave's noise-driven path wanders from its seeded entrance.
    private const float CavePathWander = 5f;
    // Gives side passages room to curve away from their junctions.
    private const float CaveBranchWander = 7f;
    // Sets the base radius of connected cave passages in blocks.
    private const float CaveTunnelRadius = 3f;
    // Lets cave walls widen and narrow with the cave noise field.
    private const float CaveRadiusVariation = 0.8f;
    // Adds finer bends to the cave path and shape without making tunnels jagged.
    private const int CaveOctaveCount = 3;
    // Limits horizontal and elevation coordinates to -256 (inclusive) through 256 (exclusive).
    private const int WorldBoundBlocks = 256;
    private const int MinChunkY = -WorldBoundBlocks / Chunk.ChunkSize;
    private const int MaxChunkY = WorldBoundBlocks / Chunk.ChunkSize - 1;
    private const int MinChunkZ = -WorldBoundBlocks / Chunk.ChunkSize;
    private const int MaxChunkZ = WorldBoundBlocks / Chunk.ChunkSize - 1;

    /// <summary>Creates a chunk with every block filled with dirt.</summary>
    public static Chunk GenerateSolidChunk()
    {
        // A new chunk starts with its default block values.
        var chunk = new Chunk();
        // Fill every coordinate so the returned chunk is completely solid.
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            // Visit every north-south coordinate in the chunk.
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                // Visit every elevation coordinate in the chunk.
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    // Set this cell to dirt so no air remains in the chunk.
                    chunk.SetBlock(x, y, z, BlockType.Dirt);
                }
            }
        }
        // Return the completed solid chunk to the caller.
        return chunk;
    }

    /// <summary>
    /// Creates a terrain chunk whose height map is determined by a 32-digit numeric seed and
    /// the chunk's world coordinates. X and Y are horizontal; Z is elevation.
    /// Non-cave terrain below elevation zero is filled with water.
    /// </summary>
    public static Chunk GenerateTerrainChunk(
        string seed,
        int chunkX = 0,
        int chunkY = 0,
        int chunkZ = 0,
        int verticalChunkCount = 4,
        int horizontalChunkCount = 4)
    {
        // A null seed cannot be checked for length or converted to bytes.
        ArgumentNullException.ThrowIfNull(seed);
        ArgumentOutOfRangeException.ThrowIfLessThan(verticalChunkCount, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(horizontalChunkCount, 1);
        // Keep both north-south and elevation coordinates inside the world's block bounds.
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkY, MinChunkY);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunkY, MaxChunkY);
        ArgumentOutOfRangeException.ThrowIfLessThan(chunkZ, MinChunkZ);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(chunkZ, MaxChunkZ);
        // Require exactly 32 numeric characters.
        if (seed.Length != 32 || !seed.All(char.IsAsciiDigit))
        {
            // Report invalid input explicitly instead of generating terrain from a bad seed.
            throw new ArgumentException(
                "The seed must contain exactly 32 numeric digits (0-9).",
                nameof(seed));
        }

        // Hash the numeric seed to derive deterministic terrain noise.
        byte[] seedHash = SHA256.HashData(Encoding.ASCII.GetBytes(seed));
        // Use the first eight hash bytes as part of the coordinate hash's seed.
        ulong seedLow = BinaryPrimitives.ReadUInt64LittleEndian(seedHash);
        // Use the next eight bytes as additional seed material for the coordinate hash.
        ulong seedHigh = BinaryPrimitives.ReadUInt64LittleEndian(seedHash.AsSpan(sizeof(ulong)));
        // Derive an independent cave seed from only the second half of the user-provided seed.
        byte[] caveSeedHash = SHA256.HashData(Encoding.ASCII.GetBytes(seed[(seed.Length / 2)..]));
        // Use the first cave hash word so cave shapes differ from the surface heightmap.
        ulong caveSeedLow = BinaryPrimitives.ReadUInt64LittleEndian(caveSeedHash);
        // Use another cave hash word to diversify the 3D cave lattice values.
        ulong caveSeedHigh = BinaryPrimitives.ReadUInt64LittleEndian(
            caveSeedHash.AsSpan(sizeof(ulong)));
        // Calculate the full horizontal extent used for the seeded cave layout.
        int worldWidth = checked(horizontalChunkCount * Chunk.ChunkSize);
        // Use the cave seed to randomize the connected network while keeping it repeatable.
        var caveRandom = new Random(BinaryPrimitives.ReadInt32LittleEndian(caveSeedHash));
        // Place several entrances in separate sections of the north-south span to avoid clustering them.
        int entranceCount = Math.Clamp(worldWidth / 16 + 1, 2, 8);
        int entranceSpacing = (worldWidth - 4) / entranceCount;
        int[] entranceXs = Enumerable.Range(0, entranceCount)
            .Select(index =>
                2 + index * entranceSpacing +
                caveRandom.Next(Math.Max(1, entranceSpacing)))
            .ToArray();
        // Scale the heightmap to the full stack so lower chunks contain terrain beneath the surface.
        int worldHeight = checked(verticalChunkCount * Chunk.ChunkSize);
        // Vary the main tunnel depth for each seed while keeping it below the surface.
        int minimumCaveLevel = Math.Max(3, worldHeight / 8);
        int maximumCaveLevel = Math.Max(minimumCaveLevel + 1, worldHeight / 4);
        int caveLevel = caveRandom.Next(minimumCaveLevel, maximumCaveLevel + 1);
        // Build randomized side passages that connect directly to the winding main tunnel.
        int branchCount = Math.Clamp(horizontalChunkCount * 2, 4, 12);
        var branches = new CaveBranch[branchCount];
        for (int i = 0; i < branches.Length; i++)
        {
            int branchX = caveRandom.Next(2, worldWidth - 2);
            int branchStartY = (int)MathF.Round(GetCaveTunnelCenterY(
                branchX, worldWidth, caveLevel, caveSeedLow, caveSeedHigh));
            int branchLength = caveRandom.Next(6, Math.Max(7, worldWidth / 2 + 1));
            int branchDirection = caveRandom.Next(2) == 0 ? -1 : 1;
            int branchEndY = Math.Clamp(
                branchStartY + branchDirection * branchLength,
                2,
                worldWidth - 3);
            if (branchEndY == branchStartY)
            {
                branchEndY = Math.Clamp(
                    branchStartY - branchDirection * branchLength,
                    2,
                    worldWidth - 3);
            }
            branches[i] = new CaveBranch(
                branchX,
                branchStartY,
                branchEndY,
                caveRandom.Next(-4, 5),
                caveRandom.Next(-10_000, 10_001),
                caveRandom.Next(-10_000, 10_001));
        }
        // Align each entrance shaft with the winding main tunnel so it joins the network.
        int[] entranceYs = entranceXs
            .Select(x => (int)MathF.Round(GetCaveTunnelCenterY(
                x, worldWidth, caveLevel, caveSeedLow, caveSeedHigh)))
            .ToArray();
        var branchCentersByY = new (float X, float Z)[Chunk.ChunkSize][];
        for (int y = 0; y < Chunk.ChunkSize; y++)
        {
            float worldBlockY = chunkY * Chunk.ChunkSize + y;
            branchCentersByY[y] = branches
                .Select(branch => GetCaveBranchCenter(
                    branch, worldBlockY, caveLevel, caveSeedLow, caveSeedHigh))
                .ToArray();
        }
        // Allocate the block grid that this method will fill.
        var chunk = new Chunk();

        // Visit each east-west column.
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            float worldBlockX = chunkX * Chunk.ChunkSize + x;
            float worldX = worldBlockX * BaseNoiseScale;
            // Visit each north-south column.
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                float worldBlockY = chunkY * Chunk.ChunkSize + y;
                float worldY = worldBlockY * BaseNoiseScale;
                // Use cave-specific noise to gently wind the connected tunnel along x.
                float tunnelCenterY = GetCaveTunnelCenterY(
                    (int)worldBlockX, worldWidth, caveLevel, caveSeedLow, caveSeedHigh);
                // Sample smooth multi-scale noise at this column using the seed hash.
                float noise = FractalNoise(worldX, worldY, seedLow, seedHigh);
                // Save the sample on the chunk so the mesher can shade this column consistently.
                chunk.SetTerrainNoise(x, y, noise);
                // Turn normalized noise into a world-space surface height.
                int surfaceHeight = Math.Clamp(
                    // Round the varied fraction of total world height to a whole block.
                    (int)MathF.Round(
                        worldHeight * (BaseTerrainHeight + noise * TerrainHeightVariation)),
                    // Keep the terrain at least one block tall.
                    1,
                    // Keep the surface inside the generated vertical chunk stack.
                    worldHeight);

                // Visit every elevation cell and account for its world-space position.
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    int worldZ = chunkZ * Chunk.ChunkSize + z;

                    if (worldZ >= surfaceHeight)
                        continue;

                    // Put grass on top, dirt in the next three layers, and stone below them.
                    BlockType block = worldZ == surfaceHeight - 1
                        ? BlockType.Grass
                        : worldZ >= surfaceHeight - 4
                            ? BlockType.Dirt
                            : BlockType.Stone;

                    // Shape the main tunnel with cave noise while preserving its connected core.
                    float caveShape = FractalCaveNoise(
                        worldBlockX * CaveNoiseScale,
                        worldBlockY * CaveNoiseScale,
                        worldZ * CaveNoiseScale,
                        caveSeedLow,
                        caveSeedHigh);
                    float tunnelDy = worldBlockY - tunnelCenterY;
                    float tunnelDz = worldZ - caveLevel;
                    float tunnelRadius = CaveTunnelRadius + caveShape * CaveRadiusVariation;
                    bool inMainTunnel =
                        tunnelDy * tunnelDy + tunnelDz * tunnelDz <= tunnelRadius * tunnelRadius;
                    // Carve curved side passages that meet the main tunnel at their starts.
                    bool inBranchTunnel = false;
                    for (int branchIndex = 0; branchIndex < branches.Length; branchIndex++)
                    {
                        CaveBranch branch = branches[branchIndex];
                        int minimumBranchY = Math.Min(branch.StartY, branch.EndY);
                        int maximumBranchY = Math.Max(branch.StartY, branch.EndY);
                        if (worldBlockY < minimumBranchY - tunnelRadius ||
                            worldBlockY > maximumBranchY + tunnelRadius)
                        {
                            continue;
                        }

                        (float branchCenterX, float branchCenterZ) = branchCentersByY[y][branchIndex];
                        float branchDx = worldBlockX - branchCenterX;
                        float branchDz = worldZ - branchCenterZ;
                        float branchRadius = CaveTunnelRadius + caveShape * CaveRadiusVariation;
                        if (branchDx * branchDx + branchDz * branchDz <=
                            branchRadius * branchRadius)
                        {
                            inBranchTunnel = true;
                            break;
                        }
                    }

                    // Carve every seeded surface shaft down to the connected cave passage.
                    bool inEntranceShaft = false;
                    for (int entrance = 0; entrance < entranceXs.Length; entrance++)
                    {
                        int entranceDx = (int)worldBlockX - entranceXs[entrance];
                        int entranceDy = (int)worldBlockY - entranceYs[entrance];
                        if (entranceDx * entranceDx + entranceDy * entranceDy <= 2 &&
                            worldZ >= caveLevel &&
                            worldZ < surfaceHeight)
                        {
                            inEntranceShaft = true;
                            break;
                        }
                    }

                    if (inMainTunnel || inBranchTunnel || inEntranceShaft)
                    {
                        // Replace tunnel and entrance cells with air.
                        chunk.SetBlock(x, y, z, BlockType.Air);
                        continue;
                    }

                    // Fill non-cave terrain below sea level with water.
                    chunk.SetBlock(x, y, z, worldZ < 0 ? BlockType.Water : block);
                }
            }
        }

        // Return the generated block grid and its saved per-column noise samples.
        return chunk;
    }

    private readonly record struct CaveBranch(
        int StartX,
        int StartY,
        int EndY,
        int LevelOffset,
        int NoiseOffsetX,
        int NoiseOffsetZ);

    // Returns a smoothly wandering branch centerline that joins the main passage at its start.
    private static (float X, float Z) GetCaveBranchCenter(
        CaveBranch branch,
        float worldY,
        int caveLevel,
        ulong seedLow,
        ulong seedHigh)
    {
        float progress = Math.Clamp(
            (worldY - branch.StartY) / (branch.EndY - branch.StartY),
            0f,
            1f);
        float noiseX = GetCaveBranchNoise(branch, progress, seedLow, seedHigh);
        float noiseZ = GetCaveBranchNoise(branch, progress, seedLow, seedHigh, swapOffsets: true);

        return (
            branch.StartX + noiseX * CaveBranchWander * progress,
            caveLevel + branch.LevelOffset * progress +
                noiseZ * CaveBranchWander * progress);
    }

    private static float GetCaveBranchNoise(
        CaveBranch branch,
        float progress,
        ulong seedLow,
        ulong seedHigh,
        bool swapOffsets = false) =>
        FractalCaveNoise(
            progress * 2.5f,
            (swapOffsets ? branch.NoiseOffsetZ : branch.NoiseOffsetX) * CaveNoiseScale,
            (swapOffsets ? branch.NoiseOffsetX : branch.NoiseOffsetZ) * CaveNoiseScale,
            seedLow,
            seedHigh);

    // Returns the seeded, gently winding centerline shared by tunnels and entrances.
    private static float GetCaveTunnelCenterY(
        int worldX,
        int worldWidth,
        int caveLevel,
        ulong seedLow,
        ulong seedHigh)
    {
        // Sample the 3D cave field along x and offset it into the world's horizontal range.
        float pathNoise = FractalCaveNoise(
            worldX * CaveNoiseScale,
            caveLevel * CaveNoiseScale,
            0,
            seedLow,
            seedHigh);
        return Math.Clamp(
            (worldWidth - 1) / 2f + pathNoise * CavePathWander,
            2f,
            worldWidth - 3f);
    }

    /// <summary>Creates an empty chunk by setting every block to air.</summary>
    public static Chunk GenerateChunk()
    {
        // Allocate a new block grid for the empty chunk.
        var chunk = new Chunk();
        // Explicitly initialize every cell as air for a predictable empty chunk.
        for (int x = 0; x < Chunk.ChunkSize; x++)
        {
            // Visit every north-south coordinate in this x slice.
            for (int y = 0; y < Chunk.ChunkSize; y++)
            {
                // Visit every elevation coordinate in this x/y column.
                for (int z = 0; z < Chunk.ChunkSize; z++)
                {
                    // Mark this cell as empty space.
                    chunk.SetBlock(x, y, z, BlockType.Air);
                }
            }
        }
        // Return the fully initialized empty chunk.
        return chunk;
    }

    // Combines smoothly sampled noise at multiple scales into a single terrain value.
    private static float FractalNoise(float x, float y, ulong seedLow, ulong seedHigh)
    {
        // Accumulates each octave's contribution before normalization.
        float total = 0;
        // Starts the largest-scale noise with full strength.
        float amplitude = 1;
        // Tracks total strength so the final result stays in the noise range.
        float amplitudeTotal = 0;

        // Add each octave, doubling its detail scale and reducing its influence.
        for (int octave = 0; octave < OctaveCount; octave++)
        {
            // Sample this scale and weight it by the current amplitude.
            total += ValueNoise(x, y, seedLow, seedHigh, octave) * amplitude;
            // Record the weight used so all contributions can be normalized afterward.
            amplitudeTotal += amplitude;
            // Double the x-coordinate frequency to add finer detail next octave.
            x *= 2;
            // Double the y-coordinate frequency to match the x detail scale.
            y *= 2;
            // Reduce the next octave's strength to keep fine details subtle.
            amplitude *= OctavePersistence;
        }

        // Divide by the sum of octave weights to return a normalized weighted average.
        return total / amplitudeTotal;
    }

    // Combines smooth 3D noise at multiple scales to form winding cave tunnels.
    private static float FractalCaveNoise(
        float x,
        float y,
        float z,
        ulong seedLow,
        ulong seedHigh)
    {
        // Accumulates each cave-noise octave before normalization.
        float total = 0;
        // Starts with the broadest cave shape at full strength.
        float amplitude = 1;
        // Tracks total strength so the final cave value stays normalized.
        float amplitudeTotal = 0;

        // Add finer tunnel bends while reducing each octave's influence.
        for (int octave = 0; octave < CaveOctaveCount; octave++)
        {
            // Sample the current 3D scale and weight it by its amplitude.
            total += ValueNoise3D(x, y, z, seedLow, seedHigh, octave) * amplitude;
            // Record the weight so the accumulated noise can be normalized.
            amplitudeTotal += amplitude;
            // Double each axis frequency to add finer tunnel detail in the next octave.
            x *= 2;
            y *= 2;
            z *= 2;
            // Reduce the finer scale's effect to keep tunnel walls smooth.
            amplitude *= 0.5f;
        }

        // Return a normalized weighted average across the cave-noise scales.
        return total / amplitudeTotal;
    }

    // Interpolates eight seeded lattice values to produce smooth noise in three dimensions.
    private static float ValueNoise3D(
        float x,
        float y,
        float z,
        ulong seedLow,
        ulong seedHigh,
        int octave)
    {
        // Find the integer grid cell containing the sample point.
        int x0 = (int)MathF.Floor(x);
        int y0 = (int)MathF.Floor(y);
        int z0 = (int)MathF.Floor(z);
        // Fade each fractional coordinate so neighboring cells join smoothly.
        float tx = Fade(x - x0);
        float ty = Fade(y - y0);
        float tz = Fade(z - z0);

        // Interpolate x along the four pairs of corners on the cell's z/y planes.
        float y0z0 = Lerp(
            LatticeValue3D(x0, y0, z0, seedLow, seedHigh, octave),
            LatticeValue3D(x0 + 1, y0, z0, seedLow, seedHigh, octave),
            tx);
        float y0z1 = Lerp(
            LatticeValue3D(x0, y0, z0 + 1, seedLow, seedHigh, octave),
            LatticeValue3D(x0 + 1, y0, z0 + 1, seedLow, seedHigh, octave),
            tx);
        float y1z0 = Lerp(
            LatticeValue3D(x0, y0 + 1, z0, seedLow, seedHigh, octave),
            LatticeValue3D(x0 + 1, y0 + 1, z0, seedLow, seedHigh, octave),
            tx);
        float y1z1 = Lerp(
            LatticeValue3D(x0, y0 + 1, z0 + 1, seedLow, seedHigh, octave),
            LatticeValue3D(x0 + 1, y0 + 1, z0 + 1, seedLow, seedHigh, octave),
            tx);

        // Interpolate along z, then y, to blend all eight corner values.
        float lowerY = Lerp(y0z0, y0z1, tz);
        float upperY = Lerp(y1z0, y1z1, tz);
        return Lerp(lowerY, upperY, ty);
    }

    // Produces a repeatable pseudo-random value for one integer 3D cave-grid coordinate.
    private static float LatticeValue3D(
        int x,
        int y,
        int z,
        ulong seedLow,
        ulong seedHigh,
        int octave)
    {
        // Mix all three coordinates, the octave, and the cave seed into one 64-bit state.
        ulong value = seedLow
            ^ ((ulong)(uint)x * 0x9E3779B185EBCA87UL)
            ^ ((ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL)
            ^ ((ulong)(uint)z * 0x165667B19E3779F9UL)
            ^ ((ulong)(uint)octave * 0x85EBCA77C2B2AE63UL)
            ^ seedHigh;
        // Fold high bits into low bits to begin dispersing the coordinate pattern.
        value ^= value >> 30;
        // Multiply by a mixing constant so nearby grid cells get unrelated values.
        value *= 0xBF58476D1CE4E5B9UL;
        // Fold the newly mixed high bits back into the state.
        value ^= value >> 27;
        // Apply another multiplication to spread the remaining coordinate influence.
        value *= 0x94D049BB133111EBUL;
        // Finalize the avalanche before converting the state to a noise value.
        value ^= value >> 31;
        // Map the upper 24 bits to the signed noise range from -1 to 1.
        return (value >> 40) / (float)0xFFFFFF * 2f - 1f;
    }

    // Computes smooth 2D value noise by interpolating four deterministic grid-corner values.
    private static float ValueNoise(float x, float y, ulong seedLow, ulong seedHigh, int octave)
    {
        // Find the integer grid cell to the lower-left of the sample point.
        int x0 = (int)MathF.Floor(x);
        // Find the integer grid cell below the sample point on the y axis.
        int y0 = (int)MathF.Floor(y);
        // Calculate the fractional x position and smooth it to avoid sharp transitions.
        float tx = Fade(x - x0);
        // Calculate and smooth the fractional y position in the same way.
        float ty = Fade(y - y0);

        // Interpolate the two corners along the top edge of the grid cell.
        float top = Lerp(
            // Value at the cell's top-left corner.
            LatticeValue(x0, y0, seedLow, seedHigh, octave),
            // Value at the neighboring corner one grid step to the right.
            LatticeValue(x0 + 1, y0, seedLow, seedHigh, octave),
            // Use the smoothed x distance to choose between those corner values.
            tx);
        // Interpolate the two corners along the bottom edge of the grid cell.
        float bottom = Lerp(
            // Value at the cell's bottom-left corner.
            LatticeValue(x0, y0 + 1, seedLow, seedHigh, octave),
            // Value at the cell's bottom-right corner.
            LatticeValue(x0 + 1, y0 + 1, seedLow, seedHigh, octave),
            // Use the same smoothed x fraction for this edge.
            tx);
        // Interpolate between the top and bottom edge values along y.
        return Lerp(top, bottom, ty);
    }

    // Produces a repeatable pseudo-random value for one integer grid coordinate.
    private static float LatticeValue(int x, int y, ulong seedLow, ulong seedHigh, int octave)
    {
        // Mix seed, signed coordinate bit patterns, and octave into a single 64-bit state.
        ulong value = seedLow
            // Spread x-coordinate differences across the state using a large odd multiplier.
            ^ ((ulong)(uint)x * 0x9E3779B185EBCA87UL)
            // Mix y independently so different grid rows get different values.
            ^ ((ulong)(uint)y * 0xC2B2AE3D27D4EB4FUL)
            // Include the octave so each detail scale has its own lattice values.
            ^ ((ulong)(uint)octave * 0x165667B19E3779F9UL)
            // Include the second seed word to use more of the SHA-256 digest.
            ^ seedHigh;
        // Begin a 64-bit avalanche mix by folding high bits into low bits.
        value ^= value >> 30;
        // Multiply by a mixing constant to disperse nearby input bit patterns.
        value *= 0xBF58476D1CE4E5B9UL;
        // Fold the newly mixed high bits back into the low bits.
        value ^= value >> 27;
        // Apply a second multiplication for additional bit diffusion.
        value *= 0x94D049BB133111EBUL;
        // Finalize the avalanche so the output depends broadly on all input bits.
        value ^= value >> 31;
        // Keep the top 24 mixed bits, map them to [0, 1], then remap that range to [-1, 1].
        return (value >> 40) / (float)0xFFFFFF * 2f - 1f;
    }

    // Smooths a fractional coordinate with the quintic fade curve used by value noise.
    private static float Fade(float value) =>
        // This curve has zero slope at 0 and 1, making adjacent cells meet smoothly.
        value * value * value * (value * (value * 6f - 15f) + 10f);

    // Returns a linear interpolation between first and second by fraction amount.
    private static float Lerp(float first, float second, float amount) =>
        // At amount 0 this is first; at 1 it is second; values between blend them.
        first + (second - first) * amount;
}
