using System;
using System.Security.Cryptography;

namespace MySerialKey.GiqhDev.Internal;

internal static class SecureRandom
{
    public static int GetInt32(int maxExclusive)
    {
        if (maxExclusive <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        }

#if NET6_0_OR_GREATER
        return RandomNumberGenerator.GetInt32(maxExclusive);
#else
        byte[] buffer = new byte[4];
        uint limit = uint.MaxValue - (uint.MaxValue % (uint)maxExclusive);
        uint value;

        using (var rng = RandomNumberGenerator.Create())
        {
            do
            {
                rng.GetBytes(buffer);
                value = BitConverter.ToUInt32(buffer, 0);
            }
            while (value >= limit);
        }

        return (int)(value % (uint)maxExclusive);
#endif
    }
}
