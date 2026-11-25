namespace WorldTile
{
    using System;
    using System.IO;
    using System.IO.Compression;
    using UnityEngine;
    using UnityEngine.Networking;

    public static class CustomDeltaApplier
    {
        private static string GetCustomEditsFolder()
        {
#if UNITY_EDITOR
            string dir = Path.Combine(Application.dataPath, "CustomEdits");
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
#else
            return Path.Combine(Application.persistentDataPath, "CustomEdits");
#endif
        }

        private static byte[] ReadCustomFileBytes(string relativePath)
        {
            string primaryPath = Path.Combine(GetCustomEditsFolder(), relativePath);
            if (File.Exists(primaryPath))
            {
                Debug.Log($"[CustomDeltaApplier] Loaded edit file from primary path: {primaryPath}");
                return File.ReadAllBytes(primaryPath);
            }

            if (relativePath.EndsWith("_delta.bytes"))
            {
                string baseName = relativePath.Substring(0, relativePath.Length - 12);
                foreach (var legacyExt in new[] { ".delta.bytes", ".delta" })
                {
                    string p1 = Path.Combine(GetCustomEditsFolder(), baseName + legacyExt);
                    if (File.Exists(p1))
                    {
                        Debug.Log($"[CustomDeltaApplier] Loaded edit file from legacy path: {p1}");
                        return File.ReadAllBytes(p1);
                    }
                }
            }

            string persistentPath = Path.Combine(Application.persistentDataPath, "CustomEdits", relativePath);
            if (File.Exists(persistentPath))
            {
                Debug.Log($"[CustomDeltaApplier] Loaded edit file from persistentDataPath: {persistentPath}");
                return File.ReadAllBytes(persistentPath);
            }

#if UNITY_ANDROID && !UNITY_EDITOR
            string streamingUrl = Path.Combine(Application.streamingAssetsPath, "CustomEdits", relativePath);
            if (!streamingUrl.Contains("://"))
                streamingUrl = "file://" + streamingUrl;

            using (var req = UnityWebRequest.Get(streamingUrl))
            {
                var asyncOp = req.SendWebRequest();
                while (!asyncOp.isDone) { }
                if (req.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log($"[CustomDeltaApplier] Loaded edit file on Android via UnityWebRequest: {streamingUrl}");
                    return req.downloadHandler.data;
                }
            }
#endif
            return null;
        }

        public static short[,] LoadDelta(int x, int y, int zoom)
        {
            string rel = $"{zoom}/{x}_{y}_delta.bytes";
            byte[] bytes = ReadCustomFileBytes(rel);
            if (bytes == null || bytes.Length == 0) return null;

            try
            {
                using MemoryStream ms = new MemoryStream(bytes);
                using DeflateStream deflate = new DeflateStream(ms, CompressionMode.Decompress);
                using BinaryReader reader = new BinaryReader(deflate);

                char[] magic = reader.ReadChars(4);
                if (new string(magic) != "DELT") return null;
                if (reader.ReadByte() != 1) return null;
                int size = reader.ReadUInt16();
                if (size != 256) return null;

                short[,] data = new short[size, size];
                for (int z = 0; z < size; z++)
                    for (int xIdx = 0; xIdx < size; xIdx++)
                        data[xIdx, z] = reader.ReadInt16();
                return data;
            }
            catch
            {
                return null;
            }
        }

        public static Texture2D LoadPaintMask(int x, int y, int zoom)
        {
            string rel = $"{zoom}/{x}_{y}_paint.png";
            byte[] bytes = ReadCustomFileBytes(rel);
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.LoadImage(bytes);
                tex.name = $"PaintMask_{x}_{y}";
                Debug.Log($"[CustomDeltaApplier] Applied paint mask to tile [{zoom}/{x}/{y}]!");
                return tex;
            }
            catch
            {
                return null;
            }
        }

        public static float[,] Apply(float[,] heightMap, int x, int y, int zoom)
        {
            var delta = LoadDelta(x, y, zoom);
            if (delta == null) return heightMap;

            int size = 256;
            var result = new float[size, size];
            for (int z = 0; z < size; z++)
                for (int xi = 0; xi < size; xi++)
                    result[xi, z] = heightMap[xi, z] + delta[xi, z];

            Debug.Log($"[CustomDeltaApplier] Applied custom delta height to tile [{zoom}/{x}/{y}]!");
            return result;
        }

        public static short[,] Apply(short[,] heightMap, int x, int y, int zoom)
        {
            var delta = LoadDelta(x, y, zoom);
            if (delta == null) return heightMap;

            int size = 256;
            var result = new short[size, size];
            for (int z = 0; z < size; z++)
                for (int xi = 0; xi < size; xi++)
                    result[xi, z] = (short)Mathf.Clamp(
                        heightMap[xi, z] + delta[xi, z], short.MinValue, short.MaxValue);

            Debug.Log($"[CustomDeltaApplier] Applied custom delta height to tile [{zoom}/{x}/{y}]!");
            return result;
        }
    }
}
