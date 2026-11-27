namespace WorldTile
{ 
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class StandartState : MonoBehaviour,  ITileStateBase
    {

        [SerializeField] TileFasade _tileFasade;

        public void Enter()
        {
            _tileFasade.TileChangeVisual.SetStandartVisual();
        }
    }
}