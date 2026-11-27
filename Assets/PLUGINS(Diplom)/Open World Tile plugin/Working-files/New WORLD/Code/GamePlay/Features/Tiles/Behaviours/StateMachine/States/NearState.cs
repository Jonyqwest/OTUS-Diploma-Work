namespace WorldTile
{
    using UnityEngine;

    public class NearState : MonoBehaviour, ITileStateBase
    {

        [SerializeField] TileFasade _tileFasade;

        public void Enter()
        {
            _tileFasade.TileChangeVisual.SetNearVisual();
        }
    }
}