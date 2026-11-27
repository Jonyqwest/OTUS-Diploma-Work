//namespace WorldTile
//{
//    public interface ITileState
//    {
//        void OnEnter(Tile tile);
//        void OnExit(Tile tile);
//        void Update(Tile tile);
//    }

//    public class TileLoadingLowState : ITileState
//    {
//        public void OnEnter(Tile tile)
//        {
//            Debug.Log($"Tile {tile.Id} → Загружаем low текстуру");
//            tile.StartCoroutine(tile.LoadLowResTexture());
//        }

//        public void OnExit(Tile tile)
//        {
//            Debug.Log($"Tile {tile.Id} покидает состояние LoadingLow");
//        }

//        public void Update(Tile tile)
//        {
//            // например, проверяем статус загрузки
//        }
//    }

//}