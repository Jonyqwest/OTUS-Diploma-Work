using System;

namespace Code.Data
{
    [Serializable]
    public class PlayerProgress
    {
        public WorldData WorldData;
        public WalletData WalletData;
        
        public PlayerProgress()
        {
            WorldData = new WorldData();
            WalletData = new WalletData();
        }
    }
}