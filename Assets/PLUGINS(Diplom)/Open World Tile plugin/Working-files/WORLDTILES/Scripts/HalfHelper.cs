using System;

public static class HalfHelper
{
    public static ushort FloatToHalf(float f)
    {
        uint fbits = BitConverter.ToUInt32(BitConverter.GetBytes(f), 0);
        uint sign = (fbits >> 16) & 0x8000;
        uint val = (fbits & 0x7fffffff) + 0x1000;
        if (val >= 0x47800000)
        {
            if ((fbits & 0x7fffffff) >= 0x47800000)
            {
                if (val < 0x7f800000)
                    return (ushort)(sign | 0x7c00); // Inf
                return (ushort)(sign | 0x7c00 | ((fbits & 0x007fffff) >> 13)); // NaN
            }
            return (ushort)(sign | 0x7bff); // Max Half Float
        }
        if (val >= 0x38800000)
            return (ushort)(sign | ((val - 0x38000000) >> 13));
        if (val < 0x33000000)
            return (ushort)(sign);
        val = (fbits & 0x7fffffff) >> 23;
        return (ushort)(sign | ((((fbits & 0x7fffff) | 0x800000) + (0x800000 >> (int)(113 - val))) >> (int)(126 - val)));
    }

    public static float HalfToFloat(ushort half)
    {
        uint sign = (uint)(half & 0x8000) << 16;
        uint exp = (uint)(half & 0x7c00) >> 10;
        uint mant = (uint)(half & 0x03ff);

        if (exp == 0)
        {
            if (mant == 0)
                return BitConverter.ToSingle(BitConverter.GetBytes(sign), 0);
            while ((mant & 0x0400) == 0)
            {
                mant <<= 1;
                exp--;
            }
            exp++;
            mant &= ~0x0400u;
        }
        else if (exp == 31)
        {
            if (mant == 0)
                return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7f800000), 0);
            else
                return BitConverter.ToSingle(BitConverter.GetBytes(sign | 0x7f800000 | (mant << 13)), 0);
        }

        exp = exp + (127 - 15);
        mant = mant << 13;

        uint f = sign | (exp << 23) | mant;
        return BitConverter.ToSingle(BitConverter.GetBytes(f), 0);
    }
}
