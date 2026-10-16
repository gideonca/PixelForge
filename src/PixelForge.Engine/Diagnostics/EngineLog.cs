using Microsoft.Extensions.Logging;

namespace PixelForge.Engine.Diagnostics;

internal static partial class EngineLog
{
    // Debug

    [LoggerMessage(
        EventId = 1100,
        Level = LogLevel.Debug,
        Message = "Chunk generated at ({X}, {Y}, {Z})")]
    public static partial void ChunkGenerated(
        ILogger logger, int x, int y, int z);

    [LoggerMessage(
        EventId = 1101,
        Level = LogLevel.Debug,
        Message = "Asset loaded: {AssetPath}")]
    public static partial void AssetLoaded(
        ILogger logger, string assetPath);

    // Information

    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "Starting game")]
    public static partial void Starting(ILogger logger);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Game stopped")]
    public static partial void Stopped(ILogger logger);

    [LoggerMessage(
        EventId = 1200,
        Level = LogLevel.Information,
        Message = "World saved: {WorldName}")]
    public static partial void WorldSaved(
        ILogger logger, string worldName);

    // Warning

    [LoggerMessage(
        EventId = 1300,
        Level = LogLevel.Warning,
        Message = "Texture missing: {AssetPath}. Using fallback texture")]
    public static partial void MissingTexture(
        ILogger logger, string assetPath);

    // Error

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "Game terminated unexpectedly")]
    public static partial void Failed(
        ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1400,
        Level = LogLevel.Error,
        Message = "Shader compilation failed: {ShaderName}. Details: {Details}")]
    public static partial void ShaderCompilationFailed(
        ILogger logger, string shaderName, string details);

    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Error,
        Message = "Failed to save world: {WorldName}")]
    public static partial void WorldSaveFailed(
        ILogger logger, string worldName, Exception exception);
}