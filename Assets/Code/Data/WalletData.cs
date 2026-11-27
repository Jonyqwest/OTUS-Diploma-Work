using System;

namespace Code.Data
{
    [Serializable]
    public class WalletData
    {
        public int Rings;
        public void AddRings(int amount)
        {
            if (amount > 0)
                Rings += amount;
        }
    }
}