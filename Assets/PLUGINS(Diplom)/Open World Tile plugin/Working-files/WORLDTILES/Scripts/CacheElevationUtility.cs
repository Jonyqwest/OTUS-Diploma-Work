//using System;
//using System.IO;
//using System.IO.Compression;
//using UnityEngine;

//public static class CacheElevationUtility
//{
//    //private const string    ELVfileExtension = ".elv";
//    //private const string    ELVMagic = "ELEV"; // 4 байта
//    //private const byte      ELVversion = 1;

//    //public static Texture2D LoadTileElevationFromCache(int x, int y, int zoom)
//    //{
//    //    string path = GetBinaryPath(x, y, zoom);
//    //    if (!File.Exists(path)) return null;

//    //    byte[] bytes = File.ReadAllBytes(path);
//    //    Texture2D tex = new Texture2D(2, 2);
//    //    tex.LoadImage(bytes);
//    //    return tex;
//    //}
//    //public static void SaveTileElevationToCache(Texture2D tex, int x, int y, int zoom)
//    //{
//    //    string path = CacheStat.GetElevationTilePath(zoom, x, y, "png");
//    //    Directory.CreateDirectory(Path.GetDirectoryName(path));
//    //    byte[] bytes = tex.EncodeToPNG();
//    //    File.WriteAllBytes(path, bytes);
//    //}
//    //public static void SaveElevationDataCompressed(float[,] data, int x, int y, int zoom)
//    //{
//    //    string path = GetBinaryPath(x, y, zoom);
//    //    Directory.CreateDirectory(Path.GetDirectoryName(path));

//    //    using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
//    //    using DeflateStream deflate = new DeflateStream(fs, System.IO.Compression.CompressionLevel.Optimal);
//    //    using BinaryWriter writer = new BinaryWriter(deflate);

//    //    int size = data.GetLength(0);

//    //    writer.Write(ELVMagic.ToCharArray());       // 4 bytes
//    //    writer.Write(ELVversion);                   // 1 byte
//    //    writer.Write((ushort)size);              // 2 bytes

//    //    for (int yIdx = 0; yIdx < size; yIdx++)
//    //        for (int xIdx = 0; xIdx < size; xIdx++)
//    //            writer.Write(data[xIdx, yIdx]);
//    //}

//    //public static float[,] LoadElevationDataCompressed(int x, int y, int zoom)
//    //{
//    //    string path = GetBinaryPath(x, y, zoom);
//    //    if (!File.Exists(path))
//    //        return null;

//    //    try
//    //    {
//    //        using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
//    //        using DeflateStream deflate = new DeflateStream(fs, CompressionMode.Decompress);
//    //        using BinaryReader reader = new BinaryReader(deflate);

//    //        char[] magic = reader.ReadChars(4);
//    //        if (new string(magic) != ELVMagic)
//    //            throw new Exception("Invalid file signature");

//    //        byte version = reader.ReadByte();
//    //        if (version != ELVversion)
//    //            throw new Exception("Unsupported version");

//    //        int size = reader.ReadUInt16();
//    //        float[,] data = new float[size, size];

//    //        for (int yIdx = 0; yIdx < size; yIdx++)
//    //            for (int xIdx = 0; xIdx < size; xIdx++)
//    //                data[xIdx, yIdx] = reader.ReadSingle();

//    //        return data;
//    //    }
//    //    catch (Exception ex)
//    //    {
//    //        Debug.LogWarning($"[ElevationCacheUtility] Error loading compressed data: {ex.Message}");
//    //        return null;
//    //    }
//    //}

//    //private static string GetBinaryPath(int x, int y, int zoom)
//    //{
//    //    string CacheFolder = CacheStat.GetElevationTilePath(zoom, x, y, ELVfileExtension);
//    //    return CacheFolder;
//    //        }
//}
