namespace PixelForge.Engine.World;

/// <summary>Identifies the material occupying a block cell in a chunk.</summary>
public enum BlockType : byte
{
    /// <summary>An empty cell that is not rendered as a solid block.</summary>
    Air,
    /// <summary>A dirt block.</summary>
    Dirt,
    /// <summary>A grass block.</summary>
    Grass,
    /// <summary>A stone block.</summary>
    Stone,
    /// <summary>A water block.</summary>
    Water
}
