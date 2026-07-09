using System.Text;

namespace MySerialKey.GiqhDev.Internal;

internal static class FixedTimeComparer
{
    public static bool Equals(string left, string right)
    {
        byte[] leftBytes = Encoding.ASCII.GetBytes(left);
        byte[] rightBytes = Encoding.ASCII.GetBytes(right);

        int difference = leftBytes.Length ^ rightBytes.Length;
        int length = leftBytes.Length < rightBytes.Length ? leftBytes.Length : rightBytes.Length;

        for (int i = 0; i < length; i++)
        {
            difference |= leftBytes[i] ^ rightBytes[i];
        }

        return difference == 0;
    }
}
