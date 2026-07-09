using System;

namespace MySerialKey.GiqhDev.Internal;

internal static class BaseAlphabetEncoder
{
    public static string Encode(byte[] bytes, string alphabet, int length)
    {
        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        byte[] working = new byte[bytes.Length];
        Buffer.BlockCopy(bytes, 0, working, 0, bytes.Length);

        var encoded = new char[length];
        int baseLength = alphabet.Length;

        for (int outputIndex = length - 1; outputIndex >= 0; outputIndex--)
        {
            int remainder = Divide(working, baseLength);
            encoded[outputIndex] = alphabet[remainder];
        }

        return new string(encoded);
    }

    private static int Divide(byte[] bytes, int divisor)
    {
        int remainder = 0;

        for (int i = 0; i < bytes.Length; i++)
        {
            int value = (remainder << 8) + bytes[i];
            bytes[i] = (byte)(value / divisor);
            remainder = value % divisor;
        }

        return remainder;
    }
}
