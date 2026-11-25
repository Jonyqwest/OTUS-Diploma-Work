namespace WorldTile
{ 
    using System;
    using System.IO;
    using UnityEngine;

    public static class LowTileLoadUtilites
    {
        private readonly static string lowTilesFolder = "LowTiles"; // Папка назначения
        private readonly static string lowTilesArchiveName = "LowTexture.zip"; // Имя архива с Low текстурами
        private readonly static string lowTextureFolderName = "LowTexture"; // Имя архива с Low текстурами
        private readonly static string archiveUrl = "https://gunas.band-art.ru/GAMES/WorldWalk/MapData/Textures/"; // URL архива
        public static bool isLowTilesExist = false;

        public static string GetLowTexServerPath()
            //=>archiveUrl;
            => Path.Combine(archiveUrl, lowTilesArchiveName);
                        
        public static string GetLowTexLocalPath()
            => Path.Combine(Application.persistentDataPath, lowTilesFolder);

        public static string GetLowTexArchiveName()
            => Path.Combine(Application.persistentDataPath, lowTilesArchiveName);

        public static string GetLowTextureFolderName()
            => Path.Combine(GetLowTexLocalPath(), lowTextureFolderName);

    }
}
