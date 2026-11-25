using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using UnityEngine;

public static class MeshCacheUtility
{
    private const string MeshCacheSubfolder = "Meshes";
    private static readonly string _meshCacheFolder;

    static MeshCacheUtility()
    {
        _meshCacheFolder = Path.Combine(Application.persistentDataPath, "TileCache", MeshCacheSubfolder);
    }

    public static string GetMeshKey(int x, int y, int zoom) => $"{zoom}_{x}_{y}";

    // Асинхронная загрузка: возвращаем MeshData (без использования Unity API).
    public static async Task<MeshData> LoadMeshDataAsync(int x, int y, int zoom)
    {
        string key = GetMeshKey(x, y, zoom);
        string path = Path.Combine(_meshCacheFolder, key + ".bin");

        if (!File.Exists(path)) return null;

        try
        {
            return await Task.Run(() =>
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                using var gz = new GZipStream(fs, CompressionMode.Decompress);
                using var reader = new BinaryReader(gz);

                int vLen = reader.ReadInt32();
                int uvLen = reader.ReadInt32();
                int triLen = reader.ReadInt32();
                int nLen = reader.ReadInt32();

                var verts = new Vector3[vLen];
                var uvs = new Vector2[uvLen];
                var tris = new int[triLen];
                var norms = new Vector3[nLen];

                for (int i = 0; i < vLen; i++)
                {
                    float x0 = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    float y0 = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    float z0 = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    verts[i] = new Vector3(x0, y0, z0);
                }

                for (int i = 0; i < uvLen; i++)
                {
                    ushort ux = reader.ReadUInt16();
                    ushort uy = reader.ReadUInt16();
                    uvs[i] = new Vector2(ux / 65535f, uy / 65535f);
                }

                for (int i = 0; i < triLen; i++)
                    tris[i] = reader.ReadInt32();

                for (int i = 0; i < nLen; i++)
                {
                    float nx = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    float ny = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    float nz = HalfHelper.HalfToFloat(reader.ReadUInt16());
                    norms[i] = new Vector3(nx, ny, nz);
                }

                return new MeshData { vertices = verts, uvs = uvs, triangles = tris, normals = norms };
            });
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[MeshCache] Load error: " + ex.Message);
            return null;
        }
    }

    // Фоновая запись: принимает абсолютный путь и массивы (не использует Unity API)
    public static void SaveMeshDataAsync_(int x, int y, int zoom, Vector3[] vertices, Vector2[] uvs, int[] triangles, Vector3[] normals)
    {
        string key = GetMeshKey(x, y, zoom);
        string folder = _meshCacheFolder;
        // Запускаем в фоне
        Task.Run(() =>
        {
            try
            {
                Directory.CreateDirectory(folder);
                string path = Path.Combine(folder, key + ".bin");
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var gz = new GZipStream(fs, System.IO.Compression.CompressionLevel.Optimal);
                using var writer = new BinaryWriter(gz);

                writer.Write(vertices.Length);
                writer.Write(uvs.Length);
                writer.Write(triangles.Length);
                writer.Write(normals != null ? normals.Length : 0);

                foreach (var v in vertices)
                {
                    writer.Write(HalfHelper.FloatToHalf(v.x));
                    writer.Write(HalfHelper.FloatToHalf(v.y));
                    writer.Write(HalfHelper.FloatToHalf(v.z));
                }

                foreach (var uv in uvs)
                {
                    ushort ux = (ushort)(Mathf.Clamp01(uv.x) * 65535f);
                    ushort uy = (ushort)(Mathf.Clamp01(uv.y) * 65535f);
                    writer.Write(ux);
                    writer.Write(uy);
                }

                foreach (var t in triangles)
                    writer.Write(t);

                if (normals != null)
                {
                    foreach (var n in normals)
                    {
                        writer.Write(HalfHelper.FloatToHalf(n.x));
                        writer.Write(HalfHelper.FloatToHalf(n.y));
                        writer.Write(HalfHelper.FloatToHalf(n.z));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogError("[MeshCache] Save error: " + ex);
            }
        });
    }
}
