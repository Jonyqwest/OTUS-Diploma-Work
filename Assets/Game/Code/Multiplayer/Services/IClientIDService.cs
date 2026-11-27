namespace Multiplayer
{
    public interface IClientIDService
    {
        ulong GetLast();
        ulong GetNext();
    }
}