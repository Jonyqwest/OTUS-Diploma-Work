namespace WorldTile
{
    
    /// <summary>
    /// Интерфейс для состояния тайла.
    /// </summary>
    public interface ITileState
    {
        /// <summary>Тип состояния.</summary>
        TileStateType StateType { get; }

        /// <summary>Вызывается при входе в это состояние.</summary>
        void OnEnter(TerrainTile tile);

        /// <summary>Вызывается при выходе из этого состояния.</summary>
        void OnExit(TerrainTile tile);
    }

    /// <summary>
    /// Контейнер состояния для одного тайла.
    /// </summary>
    /// 
    public class TileStateMachine
    {
        public ITileState CurrentState { get; private set; }

        /// <summary>Смена состояния.</summary>
        public void ChangeState(TerrainTile tile, ITileState newState)
        {
            if (tile == null)
                return;

            if (CurrentState == newState)
                return;

            CurrentState?.OnExit(tile);
            CurrentState = newState;
            CurrentState?.OnEnter(tile);
        }

        public TileStateType CurrentType => CurrentState?.StateType ?? TileStateType.None;
    }

}
