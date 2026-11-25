using UnityEngine;

public static class TextureCropperUtilite
{
    private static Material _cropMaterial;
    private const string PersistentCropName = "OWT Persistent Crop";



    public static Texture GetTextureZoomCroped(Texture outputTex, int outputZoom, WorldPosition worldPosition)
    {
        var zoom = worldPosition.zoom;
        var baseX = worldPosition.tileX;
        var baseY = worldPosition.tileY;

        if (zoom == outputZoom)
            return outputTex;   //Если кроп не нужен возвращаем исходную текстуру

        //Если КРОП — вырезаем нужную часть.
        var _parentTex = outputTex;
        //parentResult.OutputTex;
        int levelDiff = zoom - outputZoom;
        int factor = 1 << levelDiff;
        int _parentPx = _parentTex.width; // предполагается квадратный
        int _basePx = _parentPx / factor;

        // Индекс _base в parent (по оси)
        int idxX = baseX % factor;
        if (idxX < 0) idxX += factor;
        int idxY = baseY % factor;
        if (idxY < 0) idxY += factor;

        // Координаты в пикселях
        float pxX0 = (idxX * _basePx);
        float pxY0 = (_parentPx - (idxY + 1) * _basePx);  // инверсия Y для web-тайлов
        float pxXw = _basePx;
        float pxYh = _basePx;

        //TileStat.dictTiles[zoom].AddLog(baseX, baseY, "Кропаем Z"+_outputZoom+". Берем 1/"+factor+"часть. ");

        Rect uvRect = new Rect(pxX0 / _parentPx, pxY0 / _parentPx, (float)(1f / factor), (float)(1f / factor));

        //Высокоточный GPU КРОП
        return CropTextureMap(_parentTex, uvRect);
    }


    /// <summary>
    /// Возвращает кропнутую и декодированную в float[,] Terrarium высоту.
    /// </summary>
    public static Texture CropTextureMap(Texture src, Rect uvRect, bool GPU_Crop = true)
    {
        EnsureMaterial();
        if (_cropMaterial == null)
            return src;

        // добавляем 1 пиксель вправо и вверх (для теста смещения)
        float px = 1f / src.width;
        float py = 1f / src.height;
        uvRect.width += px;
        uvRect.height += py;

        // clamp чтобы не выйти за 0..1
        uvRect.width = Mathf.Min(1 - uvRect.x, uvRect.width);
        uvRect.height = Mathf.Min(1 - uvRect.y, uvRect.height);

        _cropMaterial.SetVector("_CropUV", new Vector4(uvRect.x, uvRect.y, uvRect.width, uvRect.height));

        int w = Mathf.RoundToInt(src.width * uvRect.width);
        int h = Mathf.RoundToInt(src.height * uvRect.height);

        if (GPU_Crop)
        {
            // This texture is retained by a tile for many frames, therefore it
            // must not come from Unity's temporary RenderTexture pool.
            RenderTexture persistentRt = null;
            try
            {
                persistentRt = new RenderTexture(
                    w,
                    h,
                    0,
                    RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.Linear)
                {
                    name = PersistentCropName,
                    wrapMode = TextureWrapMode.Clamp,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                persistentRt.Create();
                Graphics.Blit(src, persistentRt, _cropMaterial);

                RenderTexture result = persistentRt;
                persistentRt = null; // ownership is transferred to the caller
                return result;
            }
            finally
            {
                if (persistentRt != null)
                {
                    if (persistentRt.IsCreated())
                        persistentRt.Release();
                    Object.Destroy(persistentRt);
                }
            }
        }
        else
        {
            var temporaryRt = RenderTexture.GetTemporary(
                w,
                h,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.Linear);
            var previousActive = RenderTexture.active;
            Texture2D cropped = null;
            try
            {
                temporaryRt.wrapMode = TextureWrapMode.Clamp;
                Graphics.Blit(src, temporaryRt, _cropMaterial);
                cropped = new Texture2D(w, h, TextureFormat.RGBA32, false, true);
                RenderTexture.active = temporaryRt;
                cropped.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
                cropped.Apply(false, false);

                Texture2D result = cropped;
                cropped = null; // ownership is transferred to the caller
                return result;
            }
            finally
            {
                RenderTexture.active = previousActive;
                RenderTexture.ReleaseTemporary(temporaryRt);
                if (cropped != null)
                    Object.Destroy(cropped);
            }
        }
    }



    /// <summary>
    /// Создаёт и кэширует материал для GPU-кропа.
    /// </summary>
    private static void EnsureMaterial()
    {
        if (_cropMaterial != null)
            return;

        Shader shader = Shader.Find("Hidden/CropUV");
        if (shader == null)
        {
            Debug.LogError("TextureCropperUtilite: shader 'Hidden/CropUV' not found.");
            return;
        }

        _cropMaterial = new Material(shader)
        {
            name = "OWT Crop Material (Shared)",
            hideFlags = HideFlags.HideAndDontSave
        };
    }

     
}
