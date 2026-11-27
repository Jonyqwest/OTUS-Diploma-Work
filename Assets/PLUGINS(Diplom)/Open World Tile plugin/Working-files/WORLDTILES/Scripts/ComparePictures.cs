using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ComparePictures
{
    private static Texture2D zaglushkaTexture = null;
    private static Texture2D zaglushkaElevation = null;
    public static Color32 zaglushkaTextureColor;
    public static Color32 zaglushkaElevationColor;

    public static bool InitZaglushkaTexture(Texture2D img) {
        zaglushkaTexture = img;
        zaglushkaTextureColor = GetAverageColor(img);
        return true;
    }
    public static bool InitZaglushkaElevation(Texture2D img)
    {
        zaglushkaElevation = img;
        zaglushkaElevationColor = GetAverageColor(img);
        return true;
    }

    public static bool IsZaglushka(Texture2D img)
    {
       
        if (zaglushkaTexture == null) return false;
        Color32 imgAveColor = GetAverageColor(img);
        bool rez = ColorsAreSimilar(zaglushkaTextureColor, imgAveColor);
        return rez;
    }

    
    public static bool IsZaglushkaElevation(Texture2D img)
    {
        
        if (zaglushkaElevation == null) return false;
        Color32 imgAveColor = GetAverageColor(img);
        bool rez = ColorsAreSimilar(zaglushkaElevationColor, imgAveColor);
        return rez;
    }


    public static Color32 GetAverageColor(Texture2D tex)
    {
        if (tex == null || tex.width <= 0 || tex.height <= 0)
            return new Color32(0, 0, 0, 255);

        // GetPixels32 allocated width*height*4 bytes for every downloaded tile.
        // A regular grid is sufficient for detecting a uniform placeholder and
        // does not allocate a large temporary array.
        int samplesX = Mathf.Min(16, tex.width);
        int samplesY = Mathf.Min(16, tex.height);
        long r = 0, g = 0, b = 0;
        int count = samplesX * samplesY;

        for (int sampleY = 0; sampleY < samplesY; sampleY++)
        {
            int y = samplesY == 1 ? 0 : sampleY * (tex.height - 1) / (samplesY - 1);
            for (int sampleX = 0; sampleX < samplesX; sampleX++)
            {
                int x = samplesX == 1 ? 0 : sampleX * (tex.width - 1) / (samplesX - 1);
                Color32 pixel = tex.GetPixel(x, y);
                r += pixel.r;
                g += pixel.g;
                b += pixel.b;
            }
        }

        return new Color32(
            (byte)(r / count),
            (byte)(g / count),
            (byte)(b / count),
            255);
    }

    public static bool ColorsAreSimilar(Color32 a, Color32 b, int threshold = 10)
    {
        int dr = Mathf.Abs(a.r - b.r);
        int dg = Mathf.Abs(a.g - b.g);
        int db = Mathf.Abs(a.b - b.b);

        return (dr + dg + db) < threshold;
    }


}
