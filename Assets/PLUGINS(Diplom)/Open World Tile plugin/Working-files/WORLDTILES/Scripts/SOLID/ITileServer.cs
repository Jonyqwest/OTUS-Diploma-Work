

 

namespace WorldTile
{
    /////////Интерфейс

    //получения пути текстуры
    public interface ITextureProvider
    {
        string GetTexturePath(ZXY key);
    }


    /////////Реализация

    //получения URL на сервере ArcGIS
    class TextureServerArcGIS : ITextureProvider
    {
        private string URL = "https://services.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile";

        public string GetTexturePath(ZXY key)
        {
            string url = $"{URL}/{key.z}/{key.y}/{key.x}.png";
            return url;
        }
    }

    //получения URL на сервере высот s3.amazonaws.com
    class TextureServerAmazonaws : ITextureProvider
    {
        private string URL = "https://s3.amazonaws.com/elevation-tiles-prod/terrarium";

        public string GetTexturePath(ZXY key)
        {
            string url = $"{URL}/{key.z}/{key.x}/{key.y}.png";
            return url;
        }
    }

    //получения Path для загрузки из Сache
    class TextureChach : ITextureProvider
    {
        public string GetTexturePath(ZXY key)
        {
            string path = CacheTileUtility.GetTextureCachPath(key.x, key.y, key.z);
            return path;
        }
    }

    //получения Path для загрузки из LowTex хранилища
    class LowTextureCache : ITextureProvider
    {
        public string GetTexturePath(ZXY key)
        {
            string path = $"{LowTileLoadUtilites.GetLowTextureFolderName()}/{key.z}/{key.y}/{key.x}.jpg";
            return path;
        }
    }


}