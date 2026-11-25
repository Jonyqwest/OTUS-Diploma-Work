public static class Mathem
{
    public static int Mod(int value, int modulo)
    {
        return ((value % modulo) + modulo) % modulo;
    }
}