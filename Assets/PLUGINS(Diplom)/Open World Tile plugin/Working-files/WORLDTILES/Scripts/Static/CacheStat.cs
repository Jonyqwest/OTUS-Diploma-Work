using System.IO;


public static class CacheStat
{
    public const int MaxTextureCacheSizeMB = 500;
    public const int MaxHeightCacheSizeMB = 500;
    public const int MaxMeshCacheSizeMB = 200;
    public static bool AutoCleanOnStartup = true;
    public static bool SaveHeightAsPng = true;


    public static string CacheRoot = Path.Combine(UnityEngine.Application.persistentDataPath, "TileCache");

    public static string TextureCacheFolder =>
        Path.Combine(CacheRoot, "TexturesIMG");

    public static string HeightCacheFolder =>
        Path.Combine(CacheRoot, "HeightsIMG");

    public static string MeshCacheFolder =>
        Path.Combine(CacheRoot, "Mesh");

    public static string ElevationCacheFolder =>
        Path.Combine(CacheRoot, "Elevation");



    public static string GetTextureTilePath(int zoom, int x, int y, string fileExtension = "png")
    {
        return Path.Combine(TextureCacheFolder, $"{zoom}/{x}_{y}.{fileExtension}");
    }
    public static string GetElevationTilePath(int zoom, int x, int y, string fileExtension = "elv")
    {
        return Path.Combine(ElevationCacheFolder, $"{zoom}/{x}_{y}.{fileExtension}");
    }
    public static string GetHeightTilePath(int zoom, int x, int y, string fileExtension = "png")
    {
        return Path.Combine(HeightCacheFolder, $"{zoom}/{x}_{y}.{fileExtension}");
    }
}
