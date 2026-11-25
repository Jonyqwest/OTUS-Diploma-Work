namespace WorldTile
{ 
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class TileFasade : MonoBehaviour
    {
        [SerializeField]
        private TileStateMachine _stateMachine; //Моя стейт машина
        public TileStateMachine StateMachine
            => _stateMachine;

        [SerializeField]
        private TerrainTile _terrain; //Терраин
        public TerrainTile Terrain =>
            _terrain;


        //фичи

        //смена вида тайла в зависимости от готовности 
        [SerializeField]
        private TileChangeVisual _tileChangeVisual;
        public TileChangeVisual TileChangeVisual
            => _tileChangeVisual;
    }
}