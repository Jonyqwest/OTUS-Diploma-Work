using System;
using UnityEngine;

public static class MeshGenerator
{
    // Оригинал для float[,] (оставляем как есть)
    public static MeshData GenerateMeshData(float[,] heightMap, float tileSize, int resolution, float elevationScale)
    {
        return GenerateMeshInternal(
            (x, z) => heightMap[x, z], // лямбда доступа к float
            tileSize,
            resolution,
            elevationScale
        );
    }

    // Перегрузка для short[,]
    public static MeshData GenerateMeshData(short[,] heightMap, float tileSize, int resolution, float elevationScale)
    {
        return GenerateMeshInternal(
            (x, z) => heightMap[x, z], // лямбда доступа к short (автокаст → float)
            tileSize,
            resolution,
            elevationScale
        );
    }

    // Внутренний универсальный генератор
    private static MeshData GenerateMeshInternal(Func<int, int, float> getHeight, float tileSize, int resolution, float elevationScale)
    {
        int size = resolution;
        float stepSize = tileSize / (size - 1);

        var verts = new Vector3[size * size];
        var uvs = new Vector2[size * size];
        var tris = new int[(size - 1) * (size - 1) * 6];

        float minH = float.MaxValue, maxH = float.MinValue;
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float h = getHeight(i, j);
                if (h < minH) minH = h;
                if (h > maxH) maxH = h;
            }

        float range = (maxH - minH);
        if (Math.Abs(range) < 1e-6f) range = 1f;

        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = z * size + x;
                float elev = getHeight(x, z) * elevationScale;
                verts[idx] = new Vector3(x * stepSize, elev, z * stepSize);
                uvs[idx] = new Vector2((float)x / (size - 1), (float)z / (size - 1));
            }
        }

        int t = 0;
        for (int z = 0; z < size - 1; z++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                int i = z * size + x;
                tris[t++] = i;
                tris[t++] = i + size;
                tris[t++] = i + size + 1;

                tris[t++] = i;
                tris[t++] = i + size + 1;
                tris[t++] = i + 1;
            }
        }

        // QueueApplyMesh recalculates normals after welding. Keep MeshData.normals
        // for cache/API compatibility, but do not allocate an unused 256x256 array.
        return new MeshData { vertices = verts, uvs = uvs, triangles = tris, normals = null };
    }
}
