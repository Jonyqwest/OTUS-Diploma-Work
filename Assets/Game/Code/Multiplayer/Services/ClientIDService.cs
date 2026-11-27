namespace Multiplayer
{
    public class ClientIDService : IClientIDService
    {
        private ulong _lastID = 1;

        public ulong GetNext()
        {
            _lastID++;
            return _lastID;
        }

        public ulong GetLast() => _lastID;
    }
}