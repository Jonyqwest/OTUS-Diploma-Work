using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public static class TextureUtilite
{
    //склейка текстур


    //CPU
    public static Texture2D PixelsMergeTextures(List<(int x, int y, Texture2D tex)> tiles, int tileCount, Texture2D baseTexture = null)
    {
        if (tiles.Count == 0) return null;

        int tileSize = tiles[0].tex.width;
        int superSize = tileSize * tileCount;

        // No mip chain: the result is already a high-resolution satellite map.
        Texture2D superTex = null;
        try
        {
            superTex = new Texture2D(superSize, superSize, TextureFormat.RGBA32, false, false)
            {
                name = "OWT Super Texture"
            };

            // Если есть baseTexture и не все тайлы загружены — заполняем основу
            if (tiles.Count < tileCount * tileCount && baseTexture != null)
            {
                Texture2D scaledBase = null;
                try
                {
                    scaledBase = ScaleTexture(baseTexture, superSize, superSize);
                    var basePixels = scaledBase.GetPixels();
                    superTex.SetPixels(0, 0, superSize, superSize, basePixels);
                }
                finally
                {
                    if (scaledBase != null)
                        Object.Destroy(scaledBase);
                }
            }

            for (int i = 0; i < tiles.Count; i++)
            {
                var (x, y, tex) = tiles[i];
                if (tex == null) continue;

                var pixels = tex.GetPixels();
                superTex.SetPixels(x * tileSize, y * tileSize, tileSize, tileSize, pixels);
            }

            // The merged texture is only sampled by the GPU after this point.
            // Dropping the CPU copy roughly halves its retained memory.
            superTex.Apply(false, true);

            Texture2D result = superTex;
            superTex = null; // ownership is transferred to the caller
            return result;
        }
        finally
        {
            if (superTex != null)
                Object.Destroy(superTex);
        }
    }

    public static Texture2D ScaleTexture(Texture2D source, int targetWidth, int targetHeight)
    {
        var rt = RenderTexture.GetTemporary(targetWidth, targetHeight);
        Graphics.Blit(source, rt);
        Texture2D result = null;
        var activeRT = RenderTexture.active;
        try
        {
            result = new Texture2D(targetWidth, targetHeight, TextureFormat.RGBA32, false, false);
            RenderTexture.active = rt;
            result.ReadPixels(new Rect(0, 0, targetWidth, targetHeight), 0, 0);
            result.Apply(false, false);

            Texture2D output = result;
            result = null; // ownership is transferred to the caller
            return output;
        }
        finally
        {
            RenderTexture.active = activeRT;
            RenderTexture.ReleaseTemporary(rt);
            if (result != null)
                Object.Destroy(result);
        }
    }

    public static Texture2D ToTexture2D(Texture tex)
    {
        if (tex is Texture2D tex2D)
            return tex2D;

        if (tex is RenderTexture rt)
        {
            Texture2D result = null;
            var active = RenderTexture.active;
            try
            {
                result = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false, false);
                RenderTexture.active = rt;
                result.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                result.Apply(false, false);

                Texture2D output = result;
                result = null; // ownership is transferred to the caller
                return output;
            }
            finally
            {
                RenderTexture.active = active;
                if (result != null)
                    Object.Destroy(result);
            }
        }

        return null;
    }



    // GPU merge without GetPixels/SetPixels allocations.
    public static RenderTexture ShaderMergeTextures(
        List<(int x, int y, Texture2D tex)> tiles,
        int tileCount,
        ComputeShader mergeShader,
        Texture baseTexture = null)
    {
        if (tiles == null || tiles.Count == 0) return null;

        if (mergeShader == null)
        {
            Debug.LogWarning("mergeShader == null, fallback to CPU merge.");
            Texture2D cpu = null;
            try
            {
                cpu = PixelsMergeTextures(tiles, tileCount, baseTexture as Texture2D);
                return cpu != null ? ConvertToRenderTexture(cpu) : null;
            }
            finally
            {
                if (cpu != null)
                    Object.Destroy(cpu);
            }
        }

        // Если платформа не поддерживает ComputeShader — фоллбек
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.Log("Compute shaders not supported on this platform — using CPU fallback.");
            Texture2D cpu = null;
            try
            {
                cpu = PixelsMergeTextures(tiles, tileCount, baseTexture as Texture2D);
                return cpu != null ? ConvertToRenderTexture(cpu) : null;
            }
            finally
            {
                if (cpu != null)
                    Object.Destroy(cpu);
            }
        }

        int tileSize = tiles[0].tex.width;
        int superSize = tileSize * tileCount;
        int slices = tileCount * tileCount;
        Texture2DArray texArray = null;
        RenderTexture result = null;
        RenderTexture conversionRt = null;
        ComputeBuffer inputMaskBuffer = null;
        try
        {
            texArray = new Texture2DArray(
                tileSize,
                tileSize,
                slices,
                TextureFormat.RGBA32,
                false,
                true)
            {
                name = "OWT Super Texture Inputs"
            };

            result = new RenderTexture(
                superSize,
                superSize,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear)
            {
                name = "OWT Persistent Merge",
                enableRandomWrite = true,
                useMipMap = false,
                autoGenerateMips = false
            };
            result.Create();

            Graphics.Blit(baseTexture != null ? baseTexture : Texture2D.blackTexture, result);

            int[] inputMask = new int[slices];
            conversionRt = RenderTexture.GetTemporary(
                tileSize,
                tileSize,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);

            for (int i = 0; i < tiles.Count; i++)
            {
                var (x, y, texture) = tiles[i];
                if (texture == null || x < 0 || y < 0 || x >= tileCount || y >= tileCount)
                    continue;

                int slice = y * tileCount + x;
                Graphics.Blit(texture, conversionRt);
                Graphics.CopyTexture(conversionRt, 0, 0, texArray, slice, 0);
                inputMask[slice] = 1;
            }

            inputMaskBuffer = new ComputeBuffer(slices, sizeof(int));
            inputMaskBuffer.SetData(inputMask);

            int kernel = mergeShader.FindKernel("CSMain");
            mergeShader.SetInt("tileSize", tileSize);
            mergeShader.SetInt("tileCount", tileCount);
            mergeShader.SetTexture(kernel, "Inputs", texArray);
            mergeShader.SetTexture(kernel, "Result", result);
            mergeShader.SetBuffer(kernel, "HasInput", inputMaskBuffer);

            int groups = Mathf.CeilToInt((float)superSize / 8f);
            mergeShader.Dispatch(kernel, groups, groups, 1);

            RenderTexture output = result;
            result = null; // ownership is transferred to the caller
            return output;
        }
        finally
        {
            inputMaskBuffer?.Dispose();
            if (conversionRt != null)
                RenderTexture.ReleaseTemporary(conversionRt);
            if (texArray != null)
                Object.Destroy(texArray);
            if (result != null)
            {
                if (result.IsCreated())
                    result.Release();
                Object.Destroy(result);
            }
        }
    }



    // Вспомогалка: конвертирует Texture2D -> RenderTexture (если нужен RT для материала)
    private static RenderTexture ConvertToRenderTexture(Texture2D tex)
    {
        if (tex == null) return null;
        RenderTexture rt = null;
        try
        {
            rt = new RenderTexture(
                tex.width,
                tex.height,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear)
            {
                name = "OWT Persistent Merge",
                enableRandomWrite = false,
                useMipMap = false,
                autoGenerateMips = false
            };
            rt.Create();
            Graphics.Blit(tex, rt);

            RenderTexture result = rt;
            rt = null; // ownership is transferred to the caller
            return result;
        }
        finally
        {
            if (rt != null)
            {
                if (rt.IsCreated())
                    rt.Release();
                Object.Destroy(rt);
            }
        }
    }
}
