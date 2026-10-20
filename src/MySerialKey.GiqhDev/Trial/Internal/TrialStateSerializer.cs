using System.Text;

namespace MySerialKey.GiqhDev.Trial.Internal;

internal static class TrialStateSerializer
{
    private static readonly byte[] Marker = Encoding.ASCII.GetBytes("MST1");

    public static byte[] Serialize(TrialState state)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
        {
            writer.Write(Marker);
            writer.Write(ToUtcTicks(state.StartedAtUtc));
            writer.Write(ToUtcTicks(state.ExpiresAtUtc));
            writer.Write(ToUtcTicks(state.LastSuccessfulRunUtc));
            WriteOptional(writer, state.LastTrustedTimeUtc);
            WriteOptional(writer, state.ConvertedToLicenseAtUtc);
            writer.Flush();
            return stream.ToArray();
        }
    }

    public static TrialState Deserialize(byte[] value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        try
        {
            using (var stream = new MemoryStream(value, false))
            using (var reader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                byte[] marker = reader.ReadBytes(Marker.Length);
                if (marker.Length != Marker.Length || !marker.SequenceEqual(Marker))
                {
                    throw new FormatException("The trial-state marker is invalid.");
                }

                DateTimeOffset startedAtUtc = ReadUtc(reader);
                DateTimeOffset expiresAtUtc = ReadUtc(reader);
                DateTimeOffset lastSuccessfulRunUtc = ReadUtc(reader);
                DateTimeOffset? lastTrustedTimeUtc = ReadOptional(reader);
                DateTimeOffset? convertedToLicenseAtUtc = ReadOptional(reader);

                if (stream.Position != stream.Length)
                {
                    throw new FormatException("The trial state contains trailing data.");
                }

                ValidateChronology(
                    startedAtUtc,
                    expiresAtUtc,
                    lastSuccessfulRunUtc,
                    lastTrustedTimeUtc,
                    convertedToLicenseAtUtc);

                return new TrialState(
                    startedAtUtc,
                    expiresAtUtc,
                    lastSuccessfulRunUtc,
                    lastTrustedTimeUtc,
                    convertedToLicenseAtUtc);
            }
        }
        catch (EndOfStreamException exception)
        {
            throw new FormatException("The trial state is truncated.", exception);
        }
        catch (ArgumentOutOfRangeException exception)
        {
            throw new FormatException("The trial state contains an invalid timestamp.", exception);
        }
    }

    private static void ValidateChronology(
        DateTimeOffset startedAtUtc,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset lastSuccessfulRunUtc,
        DateTimeOffset? lastTrustedTimeUtc,
        DateTimeOffset? convertedToLicenseAtUtc)
    {
        if (expiresAtUtc <= startedAtUtc ||
            lastSuccessfulRunUtc < startedAtUtc ||
            lastSuccessfulRunUtc > expiresAtUtc)
        {
            throw new FormatException("The trial-state chronology is invalid.");
        }

        if (lastTrustedTimeUtc.HasValue && lastTrustedTimeUtc.Value < startedAtUtc)
        {
            throw new FormatException("The trusted trial timestamp is invalid.");
        }

        if (convertedToLicenseAtUtc.HasValue && convertedToLicenseAtUtc.Value < startedAtUtc)
        {
            throw new FormatException("The trial conversion timestamp is invalid.");
        }
    }

    private static long ToUtcTicks(DateTimeOffset value)
    {
        return value.UtcDateTime.Ticks;
    }

    private static DateTimeOffset ReadUtc(BinaryReader reader)
    {
        long ticks = reader.ReadInt64();
        return new DateTimeOffset(new DateTime(ticks, DateTimeKind.Utc));
    }

    private static void WriteOptional(BinaryWriter writer, DateTimeOffset? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(ToUtcTicks(value.Value));
        }
    }

    private static DateTimeOffset? ReadOptional(BinaryReader reader)
    {
        byte marker = reader.ReadByte();
        if (marker == 0)
        {
            return null;
        }

        if (marker != 1)
        {
            throw new FormatException("The optional trial timestamp marker is invalid.");
        }

        return ReadUtc(reader);
    }
}
